using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Services;

public static class CareTaskBusinessRules
{
    public static bool IsClosed(int statusId) =>
        statusId is CareTaskStatusIds.Completed or CareTaskStatusIds.Cancelled;

    public static bool IsOverdue(CareTask task, DateTime utcNow) =>
        task.DueAt.HasValue &&
        task.DueAt.Value < utcNow &&
        !IsClosed(task.CareTaskStatusId);

    public static int ResolveInitialStatus(int? caregiverId) =>
        caregiverId.HasValue ? CareTaskStatusIds.Assigned : CareTaskStatusIds.New;

    public static int ResolveStatusOnAssignment(int? caregiverId, int currentStatusId)
    {
        if (IsClosed(currentStatusId) || currentStatusId == CareTaskStatusIds.InProgress)
            return currentStatusId;

        return caregiverId.HasValue ? CareTaskStatusIds.Assigned : CareTaskStatusIds.New;
    }

    public static string? ValidateSave(SaveCareTaskDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return "Naslov zadatka je obavezan.";

        if (IsClosed(dto.CareTaskStatusId))
        {
            if (dto.CareTaskStatusId == CareTaskStatusIds.Completed)
                return "Završetak zadatka evidentira njegovatelj kroz akciju završi.";
        }

        return null;
    }

    public static string? ValidateCoordinatorUpdate(CareTask task, SaveCareTaskDto dto)
    {
        if (task.CareTaskStatusId == CareTaskStatusIds.InProgress &&
            dto.CaregiverId != task.CaregiverId)
            return "Ne može se promijeniti njegovatelj dok je zadatak u tijeku.";

        if (task.CareTaskStatusId == CareTaskStatusIds.Completed)
            return "Izvršeni zadatak nije moguće mijenjati.";

        return null;
    }

    public static string? ValidateStart(CareTask task, int employeeId)
    {
        if (task.CaregiverId != employeeId)
            return "Možeš pokrenuti samo vlastite zadatke.";

        if (task.CareTaskStatusId != CareTaskStatusIds.Assigned)
            return "Zadatak se može pokrenuti samo iz statusa Dodijeljeno.";

        return null;
    }

    public static string? ValidateComplete(CareTask task, int employeeId, CompleteCareTaskDto dto)
    {
        if (task.CaregiverId != employeeId)
            return "Možeš završiti samo vlastite zadatke.";

        if (task.CareTaskStatusId != CareTaskStatusIds.InProgress)
            return "Zadatak se može završiti samo iz statusa U tijeku.";

        if (string.IsNullOrWhiteSpace(dto.CompletionNote))
            return "Bilješka završetka je obavezna.";

        return null;
    }
}
