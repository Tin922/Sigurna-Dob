using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Services;

public static class AppUserBusinessRules
{
    public const int MinimumPasswordLength = 8;

    public static string? ValidateCreate(SaveAppUserDto dto)
    {
        var basicError = ValidateCommon(dto);
        if (basicError is not null)
            return basicError;

        if (string.IsNullOrWhiteSpace(dto.Password))
            return "Lozinka je obavezna pri kreiranju računa.";

        if (dto.Password.Length < MinimumPasswordLength)
            return $"Lozinka mora imati najmanje {MinimumPasswordLength} znakova.";

        return null;
    }

    public static string? ValidateUpdate(SaveAppUserDto dto)
    {
        var basicError = ValidateCommon(dto);
        if (basicError is not null)
            return basicError;

        if (!string.IsNullOrWhiteSpace(dto.Password) &&
            dto.Password.Length < MinimumPasswordLength)
            return $"Lozinka mora imati najmanje {MinimumPasswordLength} znakova.";

        return null;
    }

    public static string? ValidateProfileLink(
        IReadOnlyCollection<string> roleNames,
        int? employeeId,
        int? familyContactId)
    {
        if (roleNames.Contains(AppRoles.FamilyMember) && !familyContactId.HasValue)
            return "Uloga člana obitelji zahtijeva povezani obiteljski kontakt.";

        if ((roleNames.Contains(AppRoles.Coordinator) || roleNames.Contains(AppRoles.Caregiver)) &&
            !employeeId.HasValue)
            return "Uloga koordinatora ili njegovatelja zahtijeva povezanog djelatnika.";

        return null;
    }

    private static string? ValidateCommon(SaveAppUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return "Email je obavezan.";

        if (string.IsNullOrWhiteSpace(dto.DisplayName))
            return "Prikazano ime je obavezno.";

        if (dto.RoleIds.Count == 0)
            return "Odaberite barem jednu ulogu.";

        if (dto.EmployeeId.HasValue && dto.FamilyContactId.HasValue)
            return "Račun se može povezati samo s djelatnikom ili obiteljskim kontaktom.";

        return null;
    }
}
