using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Api.Security;
using SigurnaDob.Api.Services;
using SigurnaDob.Shared.Dtos;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly SigurnaDobDbContext _context;
    private readonly RoomOccupancyService _roomOccupancyService;

    public RoomsController(
        SigurnaDobDbContext context,
        RoomOccupancyService roomOccupancyService)
    {
        _context = context;
        _roomOccupancyService = roomOccupancyService;
    }

    [HttpGet("occupancy")]
    public async Task<ActionResult<RoomOccupancyOverviewDto>> GetOccupancyOverview() =>
        Ok(await _roomOccupancyService.GetOverviewAsync());

    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetRooms(
        [FromQuery] string? search,
        [FromQuery] int? statusId,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc")
    {
        var query = _context.Rooms
            .AsNoTracking()
            .Include(room => room.RoomStatus)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(room => room.Name.ToLower().Contains(term));
        }

        if (statusId.HasValue)
            query = query.Where(room => room.RoomStatusId == statusId.Value);

        var rooms = await query.ToListAsync();
        var roomIds = rooms.Select(room => room.Id).ToList();

        var occupancyByRoom = await _context.Residents
            .AsNoTracking()
            .Where(resident =>
                resident.RoomId != null &&
                roomIds.Contains(resident.RoomId.Value) &&
                resident.ResidentStatusId != Shared.Constants.ResidentStatusIds.MovedOut &&
                resident.ResidentStatusId != Shared.Constants.ResidentStatusIds.Archived)
            .GroupBy(resident => resident.RoomId!.Value)
            .Select(group => new { RoomId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoomId, item => item.Count);

        var result = rooms
            .Select(room =>
            {
                var occupancy = occupancyByRoom.GetValueOrDefault(room.Id);
                return ToDto(room, occupancy);
            })
            .ToList();

        result = ApplySorting(result, sortBy, sortDir).ToList();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDetailDto>> GetRoomById(int id)
    {
        var detail = await BuildDetailDto(id);
        return detail is null ? NotFound() : Ok(detail);
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<RoomDetailDto>> CreateRoom(SaveRoomDto dto)
    {
        var validationError = await ValidateSaveRequest(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        var room = new Room
        {
            Name = dto.Name.Trim(),
            Capacity = dto.Capacity,
            RoomStatusId = dto.RoomStatusId,
            Note = NormalizeOptional(dto.Note)
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoomById), new { id = room.Id }, await LoadDetailDto(room.Id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomDetailDto>> UpdateRoom(int id, SaveRoomDto dto)
    {
        var room = await _context.Rooms.FirstOrDefaultAsync(item => item.Id == id);
        if (room is null)
            return NotFound();

        var validationError = await ValidateSaveRequest(dto, id);
        if (validationError is not null)
            return BadRequest(validationError);

        var duplicateName = await _context.Rooms
            .AnyAsync(item => item.Id != id && item.Name == dto.Name.Trim());
        if (duplicateName)
            return BadRequest("Soba s tim nazivom već postoji.");

        room.Name = dto.Name.Trim();
        room.Capacity = dto.Capacity;
        room.RoomStatusId = dto.RoomStatusId;
        room.Note = NormalizeOptional(dto.Note);

        await _context.SaveChangesAsync();

        return Ok(await LoadDetailDto(id));
    }

    private async Task<string?> ValidateSaveRequest(SaveRoomDto dto, int? roomId = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Naziv sobe je obavezan.";

        if (roomId is null)
        {
            var nameExists = await _context.Rooms.AnyAsync(room => room.Name == dto.Name.Trim());
            if (nameExists)
                return "Soba s tim nazivom već postoji.";
        }

        var statusExists = await _context.RoomStatuses
            .AnyAsync(status => status.Id == dto.RoomStatusId);
        if (!statusExists)
            return "Odabrani status sobe ne postoji.";

        var occupancy = roomId is int id
            ? await RoomBusinessRules.GetOccupancyAsync(_context, id)
            : 0;

        var capacityError = RoomBusinessRules.ValidateCapacityChange(dto.Capacity, occupancy);
        if (capacityError is not null)
            return capacityError;

        var statusError = RoomBusinessRules.ValidateStatusChange(dto.RoomStatusId, occupancy);
        if (statusError is not null)
            return statusError;

        return null;
    }

    private async Task<RoomDetailDto?> BuildDetailDto(int id)
    {
        var room = await _context.Rooms
            .AsNoTracking()
            .Include(item => item.RoomStatus)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (room is null)
            return null;

        var occupancy = await RoomBusinessRules.GetOccupancyAsync(_context, id);

        var residents = await RoomBusinessRules.OccupyingResidentsQuery(_context, id)
            .OrderBy(resident => resident.LastName)
            .ThenBy(resident => resident.FirstName)
            .Select(resident => new LookupDto
            {
                Id = resident.Id,
                Name = $"{resident.FirstName} {resident.LastName}"
            })
            .ToListAsync();

        return new RoomDetailDto
        {
            Id = room.Id,
            Name = room.Name,
            Capacity = room.Capacity,
            RoomStatusId = room.RoomStatusId,
            StatusName = room.RoomStatus?.Name ?? string.Empty,
            Occupancy = occupancy,
            AvailableSpots = Math.Max(room.Capacity - occupancy, 0),
            Note = room.Note,
            Residents = residents
        };
    }

    private async Task<RoomDetailDto> LoadDetailDto(int id) =>
        (await BuildDetailDto(id))!;

    private static RoomDto ToDto(Room room, int occupancy) =>
        new()
        {
            Id = room.Id,
            Name = room.Name,
            Capacity = room.Capacity,
            RoomStatusId = room.RoomStatusId,
            StatusName = room.RoomStatus?.Name ?? string.Empty,
            Occupancy = occupancy,
            AvailableSpots = Math.Max(room.Capacity - occupancy, 0),
            Note = room.Note
        };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IEnumerable<RoomDto> ApplySorting(
        List<RoomDto> rooms,
        string sortBy,
        string sortDir)
    {
        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("capacity", false) => rooms.OrderBy(room => room.Capacity),
            ("capacity", true) => rooms.OrderByDescending(room => room.Capacity),
            ("occupancy", false) => rooms.OrderBy(room => room.Occupancy),
            ("occupancy", true) => rooms.OrderByDescending(room => room.Occupancy),
            ("status", false) => rooms.OrderBy(room => room.StatusName),
            ("status", true) => rooms.OrderByDescending(room => room.StatusName),
            ("name", true) => rooms.OrderByDescending(room => room.Name),
            _ => rooms.OrderBy(room => room.Name)
        };
    }
}
