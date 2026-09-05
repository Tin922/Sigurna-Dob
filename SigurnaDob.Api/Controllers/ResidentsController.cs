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
public class ResidentsController : ControllerBase
{
    private readonly SigurnaDobDbContext _context;
    private readonly ChangeHistoryService _changeHistory;

    public ResidentsController(
        SigurnaDobDbContext context,
        ChangeHistoryService changeHistory)
    {
        _context = context;
        _changeHistory = changeHistory;
    }

    [HttpGet]
    public async Task<ActionResult<List<ResidentDto>>> GetResidents(
        [FromQuery] string? search,
        [FromQuery] int? statusId,
        [FromQuery] int? roomId,
        [FromQuery] string sortBy = "lastName",
        [FromQuery] string sortDir = "asc")
    {
        var query = _context.Residents
            .AsNoTracking()
            .Include(resident => resident.ResidentStatus)
            .Include(resident => resident.Room)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(resident =>
                resident.FirstName.ToLower().Contains(term) ||
                resident.LastName.ToLower().Contains(term));
        }

        if (statusId.HasValue)
            query = query.Where(resident => resident.ResidentStatusId == statusId.Value);

        if (roomId.HasValue)
            query = query.Where(resident => resident.RoomId == roomId.Value);

        query = ApplySorting(query, sortBy, sortDir);

        var residents = await query
            .Select(resident => new ResidentDto
            {
                Id = resident.Id,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                DateOfBirth = resident.DateOfBirth,
                ResidentStatusId = resident.ResidentStatusId,
                StatusName = resident.ResidentStatus != null
                    ? resident.ResidentStatus.Name
                    : string.Empty,
                RoomId = resident.RoomId,
                RoomName = resident.Room != null ? resident.Room.Name : null,
                AdmittedAt = resident.AdmittedAt,
                ArchivedAt = resident.ArchivedAt
            })
            .ToListAsync();

        return Ok(residents);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResidentDetailDto>> GetResidentById(int id)
    {
        var resident = await _context.Residents
            .AsNoTracking()
            .Include(item => item.ResidentStatus)
            .Include(item => item.Room)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (resident is null)
            return NotFound();

        var familyContactCount = await _context.FamilyContacts
            .CountAsync(contact => contact.ResidentId == id);

        return Ok(ToDetailDto(resident, familyContactCount));
    }

    [HttpGet("{id:int}/status-history")]
    public async Task<ActionResult<List<ResidentStatusHistoryDto>>> GetResidentStatusHistory(int id)
    {
        var exists = await _context.Residents.AsNoTracking().AnyAsync(resident => resident.Id == id);
        if (!exists)
            return NotFound();

        return Ok(await _changeHistory.GetResidentStatusHistoryAsync(id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<ResidentDetailDto>> CreateResident(SaveResidentDto dto)
    {
        var validationError = await ValidateSaveRequest(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        var resident = new Resident
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            DateOfBirth = dto.DateOfBirth,
            PersonalId = NormalizeOptional(dto.PersonalId),
            ResidentStatusId = dto.ResidentStatusId,
            RoomId = dto.RoomId,
            AdmittedAt = dto.AdmittedAt,
            Note = NormalizeOptional(dto.Note)
        };

        ResidentBusinessRules.ApplyStatusSideEffects(resident);

        _context.Residents.Add(resident);
        await _context.SaveChangesAsync();

        _changeHistory.RecordResidentStatusChange(
            resident.Id,
            null,
            resident.ResidentStatusId,
            User);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetResidentById),
            new { id = resident.Id },
            await LoadDetailDto(resident.Id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ResidentDetailDto>> UpdateResident(
        int id,
        SaveResidentDto dto)
    {
        var resident = await _context.Residents.FirstOrDefaultAsync(item => item.Id == id);
        if (resident is null)
            return NotFound();

        var validationError = await ValidateSaveRequest(dto, id);
        if (validationError is not null)
            return BadRequest(validationError);

        var previousStatusId = resident.ResidentStatusId;

        resident.FirstName = dto.FirstName.Trim();
        resident.LastName = dto.LastName.Trim();
        resident.DateOfBirth = dto.DateOfBirth;
        resident.PersonalId = NormalizeOptional(dto.PersonalId);
        resident.ResidentStatusId = dto.ResidentStatusId;
        resident.RoomId = dto.RoomId;
        resident.AdmittedAt = dto.AdmittedAt;
        resident.Note = NormalizeOptional(dto.Note);

        ResidentBusinessRules.ApplyStatusSideEffects(resident);

        _changeHistory.RecordResidentStatusChange(
            resident.Id,
            previousStatusId,
            resident.ResidentStatusId,
            User);

        await _context.SaveChangesAsync();

        return Ok(await LoadDetailDto(id));
    }

    private async Task<string?> ValidateSaveRequest(SaveResidentDto dto, int? residentId = null)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return "Ime i prezime su obavezni.";

        var statusExists = await _context.ResidentStatuses
            .AnyAsync(status => status.Id == dto.ResidentStatusId);
        if (!statusExists)
            return "Odabrani status korisnika ne postoji.";

        var assignmentError = ResidentBusinessRules.ValidateAssignment(
            dto.ResidentStatusId,
            dto.RoomId,
            residentId);
        if (assignmentError is not null)
            return assignmentError;

        if (dto.RoomId is int roomId)
        {
            var roomExists = await _context.Rooms.AnyAsync(room => room.Id == roomId);
            if (!roomExists)
                return "Odabrana soba ne postoji.";

            var capacityError = await ResidentBusinessRules.ValidateRoomCapacityAsync(
                _context,
                roomId,
                residentId);
            if (capacityError is not null)
                return capacityError;
        }

        return null;
    }

    private async Task<ResidentDetailDto> LoadDetailDto(int id)
    {
        var resident = await _context.Residents
            .AsNoTracking()
            .Include(item => item.ResidentStatus)
            .Include(item => item.Room)
            .FirstAsync(item => item.Id == id);

        var familyContactCount = await _context.FamilyContacts
            .CountAsync(contact => contact.ResidentId == id);

        return ToDetailDto(resident, familyContactCount);
    }

    private static ResidentDetailDto ToDetailDto(Resident resident, int familyContactCount) =>
        new()
        {
            Id = resident.Id,
            FirstName = resident.FirstName,
            LastName = resident.LastName,
            DateOfBirth = resident.DateOfBirth,
            PersonalId = resident.PersonalId,
            ResidentStatusId = resident.ResidentStatusId,
            StatusName = resident.ResidentStatus?.Name ?? string.Empty,
            RoomId = resident.RoomId,
            RoomName = resident.Room?.Name,
            AdmittedAt = resident.AdmittedAt,
            ArchivedAt = resident.ArchivedAt,
            Note = resident.Note,
            FamilyContactCount = familyContactCount
        };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<Resident> ApplySorting(
        IQueryable<Resident> query,
        string sortBy,
        string sortDir)
    {
        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("firstname", false) => query.OrderBy(resident => resident.FirstName),
            ("firstname", true) => query.OrderByDescending(resident => resident.FirstName),
            ("admittedat", false) => query.OrderBy(resident => resident.AdmittedAt),
            ("admittedat", true) => query.OrderByDescending(resident => resident.AdmittedAt),
            ("status", false) => query.OrderBy(resident => resident.ResidentStatus!.Name),
            ("status", true) => query.OrderByDescending(resident => resident.ResidentStatus!.Name),
            ("lastname", true) => query.OrderByDescending(resident => resident.LastName),
            _ => query.OrderBy(resident => resident.LastName)
        };
    }
}
