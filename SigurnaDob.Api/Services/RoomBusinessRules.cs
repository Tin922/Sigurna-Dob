using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Services;

public static class RoomBusinessRules
{
    public static IQueryable<Resident> OccupyingResidentsQuery(
        SigurnaDobDbContext db,
        int roomId,
        int? excludeResidentId = null)
    {
        return db.Residents
            .AsNoTracking()
            .Where(resident =>
                resident.RoomId == roomId &&
                resident.ResidentStatusId != ResidentStatusIds.MovedOut &&
                resident.ResidentStatusId != ResidentStatusIds.Archived &&
                (excludeResidentId == null || resident.Id != excludeResidentId));
    }

    public static Task<int> GetOccupancyAsync(
        SigurnaDobDbContext db,
        int roomId,
        int? excludeResidentId = null,
        CancellationToken cancellationToken = default) =>
        OccupyingResidentsQuery(db, roomId, excludeResidentId)
            .CountAsync(cancellationToken);

    public static string? ValidateAvailableForAssignment(int roomStatusId)
    {
        if (roomStatusId is RoomStatusIds.Maintenance or RoomStatusIds.OutOfService)
            return "Korisnik se ne može smjestiti u sobu koja je u održavanju ili izvan uporabe.";

        return null;
    }

    public static string? ValidateCapacityChange(int capacity, int occupancy)
    {
        if (capacity < 1)
            return "Kapacitet sobe mora biti najmanje 1.";

        if (capacity < occupancy)
            return $"Kapacitet ({capacity}) ne smije biti manji od trenutačne popunjenosti ({occupancy}).";

        return null;
    }

    public static string? ValidateStatusChange(int roomStatusId, int occupancy)
    {
        var assignmentError = ValidateAvailableForAssignment(roomStatusId);
        if (assignmentError is not null && occupancy > 0)
            return "Soba s korisnicima mora biti u statusu U uporabi.";

        return null;
    }
}
