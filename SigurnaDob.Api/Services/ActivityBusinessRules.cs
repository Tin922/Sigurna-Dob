using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Services;

public static class ActivityBusinessRules
{
    public static string? ValidateSave(SaveActivityDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return "Naslov aktivnosti je obavezan.";

        if (dto.EndsAt.HasValue && dto.EndsAt.Value <= dto.StartsAt)
            return "Vrijeme završetka mora biti nakon početka aktivnosti.";

        return null;
    }

    public static bool IsResidentEligibleForEnrollment(Resident resident) =>
        resident.ResidentStatusId is not (
            ResidentStatusIds.MovedOut or ResidentStatusIds.Archived);

    public static string? ValidateEnrollment(Resident? resident, bool alreadyEnrolled)
    {
        if (resident is null)
            return "Odabrani korisnik doma ne postoji.";

        if (!IsResidentEligibleForEnrollment(resident))
            return "Korisnik s ovim statusom ne može sudjelovati u aktivnosti.";

        if (alreadyEnrolled)
            return "Korisnik je već evidentiran kao sudionik.";

        return null;
    }
}
