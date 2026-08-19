using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Services;

public static class ResidentBusinessRules
{
    public static string? ValidateAssignment(
        int residentStatusId,
        int? roomId,
        int? excludeResidentId = null)
    {
        if (residentStatusId == ResidentStatusIds.Active && roomId is null)
            return "Aktivan korisnik mora imati dodijeljenu sobu.";

        if (residentStatusId is ResidentStatusIds.MovedOut or ResidentStatusIds.Archived
            && roomId is not null)
            return "Premješten ili arhiviran korisnik ne smije imati aktivnu sobu.";

        return null;
    }

    public static async Task<string?> ValidateRoomCapacityAsync(
        SigurnaDobDbContext db,
        int roomId,
        int? excludeResidentId = null,
        CancellationToken cancellationToken = default)
    {
        var room = await db.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == roomId, cancellationToken);

        if (room is null)
            return "Odabrana soba ne postoji.";

        var occupancy = await db.Residents
            .AsNoTracking()
            .Where(resident =>
                resident.RoomId == roomId &&
                resident.ResidentStatusId != ResidentStatusIds.MovedOut &&
                resident.ResidentStatusId != ResidentStatusIds.Archived &&
                (excludeResidentId == null || resident.Id != excludeResidentId))
            .CountAsync(cancellationToken);

        if (occupancy >= room.Capacity)
            return $"Soba '{room.Name}' je popunjena (kapacitet {room.Capacity}).";

        return null;
    }

    public static void ApplyStatusSideEffects(Resident resident)
    {
        if (resident.ResidentStatusId is ResidentStatusIds.MovedOut or ResidentStatusIds.Archived)
            resident.RoomId = null;

        if (resident.ResidentStatusId == ResidentStatusIds.Archived)
            resident.ArchivedAt ??= DateTime.UtcNow;
        else
            resident.ArchivedAt = null;
    }
}
