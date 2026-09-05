using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Services;

public class RoomOccupancyService
{
    private readonly SigurnaDobDbContext _context;

    public RoomOccupancyService(SigurnaDobDbContext context)
    {
        _context = context;
    }

    public async Task<RoomOccupancyOverviewDto> GetOverviewAsync()
    {
        var rooms = await _context.Rooms
            .AsNoTracking()
            .Include(room => room.RoomStatus)
            .OrderBy(room => room.Name)
            .ToListAsync();

        var occupants = await _context.Residents
            .AsNoTracking()
            .Include(resident => resident.ResidentStatus)
            .Where(resident =>
                resident.RoomId != null &&
                resident.ResidentStatusId != ResidentStatusIds.MovedOut &&
                resident.ResidentStatusId != ResidentStatusIds.Archived)
            .ToListAsync();

        var occupantsByRoom = occupants
            .GroupBy(resident => resident.RoomId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(resident => resident.LastName)
                    .ThenBy(resident => resident.FirstName)
                    .Select(ToOccupantDto)
                    .ToList());

        var items = rooms
            .Select(room =>
            {
                var roomOccupants = occupantsByRoom.GetValueOrDefault(room.Id) ?? new List<RoomOccupantDto>();
                var occupancy = roomOccupants.Count;
                var availableSpots = Math.Max(room.Capacity - occupancy, 0);
                var percent = room.Capacity <= 0
                    ? 0
                    : (int)Math.Min(100, Math.Round(occupancy * 100.0 / room.Capacity));

                return new RoomOccupancyItemDto
                {
                    Id = room.Id,
                    Name = room.Name,
                    RoomStatusId = room.RoomStatusId,
                    StatusName = room.RoomStatus?.Name ?? string.Empty,
                    Capacity = room.Capacity,
                    Occupancy = occupancy,
                    AvailableSpots = availableSpots,
                    OccupancyPercent = percent,
                    IsFull = occupancy >= room.Capacity,
                    Occupants = roomOccupants
                };
            })
            .OrderByDescending(item => item.OccupancyPercent)
            .ThenBy(item => item.Name)
            .ToList();

        var inUseRooms = items.Where(room => room.RoomStatusId == RoomStatusIds.InUse).ToList();

        return new RoomOccupancyOverviewDto
        {
            Summary = new RoomOccupancySummaryDto
            {
                TotalRooms = items.Count,
                RoomsInUse = inUseRooms.Count,
                TotalCapacity = inUseRooms.Sum(room => room.Capacity),
                TotalOccupancy = inUseRooms.Sum(room => room.Occupancy),
                AvailableSpots = inUseRooms.Sum(room => room.AvailableSpots),
                FullRooms = inUseRooms.Count(room => room.IsFull),
                EmptyRooms = inUseRooms.Count(room => room.Occupancy == 0)
            },
            Rooms = items
        };
    }

    private static RoomOccupantDto ToOccupantDto(Resident resident) =>
        new()
        {
            Id = resident.Id,
            FirstName = resident.FirstName,
            LastName = resident.LastName,
            StatusName = resident.ResidentStatus?.Name ?? string.Empty
        };
}
