using System.Globalization;
using System.Text;
using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Services.Ai;

public class MockAiService : IAiService
{
    public string ProviderName => "Mock";

    public Task<string> GenerateOperationalSummaryAsync(
        AiOperationalContextDto context,
        CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Sažetak operativnog stanja (Mock AI):");
        builder.AppendLine();
        builder.AppendLine(
            $"Trenutno je otvoreno {context.OpenCareTasks} zadataka skrbi, " +
            $"od čega {context.OverdueCareTasks} kasni s rokom. " +
            $"Na odluku čeka {context.PendingVisitRequests} zahtjeva za posjet.");

        if (context.OpenTasks.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Prioritetni zadaci:");
            foreach (var task in context.OpenTasks.Take(5))
            {
                var due = task.DueAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                          ?? "bez roka";
                var overdue = task.IsOverdue ? " [KASNI]" : string.Empty;
                builder.AppendLine(
                    $"- {task.Title} ({task.ResidentName}, {task.TypeName}, rok {due}){overdue}");
            }
        }

        if (context.PendingVisits.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Posjeti na odluku:");
            foreach (var visit in context.PendingVisits.Take(5))
            {
                builder.AppendLine(
                    $"- {visit.ResidentName}: posjet od {visit.FamilyContactName} " +
                    $"({visit.RequestedVisitAt.ToLocalTime():dd.MM.yyyy HH:mm})");
            }
        }

        if (context.OverdueCareTasks > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Preporuka: prvo riješite zakašnjele zadatke i provjerite dodjele njegovatelja.");
        }

        return Task.FromResult(builder.ToString().Trim());
    }

    public Task<AiCareTaskSuggestionDto> SuggestCareTaskAsync(
        AiCareTaskSuggestionContextDto context,
        CancellationToken cancellationToken = default)
    {
        var note = context.Note.Trim();
        var noteLower = note.ToLowerInvariant();

        var resident = MatchResident(context.Residents, noteLower)
                       ?? context.Residents.FirstOrDefault()
                       ?? new LookupDto { Id = 0, Name = "Nepoznat korisnik" };

        var type = MatchCareTaskType(context.CareTaskTypes, noteLower)
                   ?? context.CareTaskTypes.FirstOrDefault()
                   ?? new LookupDto { Id = 1, Name = "Terapija" };

        var caregiver = MatchCaregiver(context.Caregivers, noteLower);

        var title = BuildTitle(note, resident.Name, type.Name);
        var dueAt = BuildDueAt(noteLower);

        var suggestion = new AiCareTaskSuggestionDto
        {
            Title = title,
            Description = note,
            ResidentId = resident.Id,
            ResidentName = resident.Name,
            CareTaskTypeId = type.Id,
            TypeName = type.Name,
            CaregiverId = caregiver?.Id,
            CaregiverName = caregiver?.Name,
            DueAt = dueAt
        };

        return Task.FromResult(suggestion);
    }

    private static LookupDto? MatchResident(IReadOnlyList<LookupDto> residents, string noteLower)
    {
        foreach (var resident in residents)
        {
            foreach (var part in resident.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.Length >= 3 && noteLower.Contains(part.ToLowerInvariant()))
                    return resident;
            }
        }

        return null;
    }

    private static LookupDto? MatchCareTaskType(IReadOnlyList<LookupDto> types, string noteLower)
    {
        var keywordMap = new Dictionary<int, string[]>
        {
            [1] = ["terapij", "lijek", "medikament", "tableta"],
            [2] = ["prehran", "obrok", "ručak", "večera", "doručak", "jelo", "hrana"],
            [3] = ["higijen", "kupanje", "tuš", "perilica", "obuć"],
            [4] = ["pratnja", "šetnj", "prosetati", "odvesti"],
            [5] = ["administr", "dokument", "obrazac", "telefon"]
        };

        foreach (var (typeId, keywords) in keywordMap)
        {
            if (keywords.Any(noteLower.Contains))
            {
                return types.FirstOrDefault(type => type.Id == typeId)
                       ?? types.FirstOrDefault(type =>
                           type.Name.Contains(keywords[0], StringComparison.OrdinalIgnoreCase));
            }
        }

        return null;
    }

    private static LookupDto? MatchCaregiver(IReadOnlyList<LookupDto> caregivers, string noteLower)
    {
        foreach (var caregiver in caregivers)
        {
            foreach (var part in caregiver.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.Length >= 3 && noteLower.Contains(part.ToLowerInvariant()))
                    return caregiver;
            }
        }

        return caregivers.FirstOrDefault();
    }

    private static string BuildTitle(string note, string residentName, string typeName)
    {
        if (string.IsNullOrWhiteSpace(note))
            return $"Zadatak skrbi — {residentName}";

        var firstSentence = note.Split(['.', '!', '?', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim();

        if (!string.IsNullOrWhiteSpace(firstSentence) && firstSentence.Length <= 80)
            return char.ToUpper(firstSentence[0]) + firstSentence[1..];

        return $"{typeName} — {residentName}";
    }

    private static DateTime? BuildDueAt(string noteLower)
    {
        var localDate = noteLower.Contains("sutra")
            ? DateTime.Today.AddDays(1)
            : noteLower.Contains("danas")
                ? DateTime.Today
                : DateTime.Today.AddDays(2);

        var localDateTime = DateTime.SpecifyKind(localDate.AddHours(10), DateTimeKind.Local);
        return localDateTime.ToUniversalTime();
    }
}
