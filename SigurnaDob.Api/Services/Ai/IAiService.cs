using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Services.Ai;

public interface IAiService
{
    string ProviderName { get; }

    Task<string> GenerateOperationalSummaryAsync(
        AiOperationalContextDto context,
        CancellationToken cancellationToken = default);

    Task<AiCareTaskSuggestionDto> SuggestCareTaskAsync(
        AiCareTaskSuggestionContextDto context,
        CancellationToken cancellationToken = default);
}
