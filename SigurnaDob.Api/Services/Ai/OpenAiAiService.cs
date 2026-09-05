using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SigurnaDob.Api.Configuration;
using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Services.Ai;

public class OpenAiAiService : IAiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly MockAiService _fallback;

    public OpenAiAiService(HttpClient httpClient, IOptions<AiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _fallback = new MockAiService();
    }

    public string ProviderName => "OpenAI";

    public async Task<string> GenerateOperationalSummaryAsync(
        AiOperationalContextDto context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return await _fallback.GenerateOperationalSummaryAsync(context, cancellationToken);

        var payload = new
        {
            model = _options.Model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "Piši sažetke na hrvatskom jeziku za koordinatora doma za starije osobe. " +
                        "Budi konkretan, kratak i naglasi prioritete."
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(context)
                }
            }
        };

        var responseText = await SendChatAsync(payload, cancellationToken);
        return string.IsNullOrWhiteSpace(responseText)
            ? await _fallback.GenerateOperationalSummaryAsync(context, cancellationToken)
            : responseText.Trim();
    }

    public async Task<AiCareTaskSuggestionDto> SuggestCareTaskAsync(
        AiCareTaskSuggestionContextDto context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return await _fallback.SuggestCareTaskAsync(context, cancellationToken);

        var payload = new
        {
            model = _options.Model,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "Vrati JSON objekt s poljima: title, description, residentId, careTaskTypeId, caregiverId (nullable), dueAt (ISO 8601 UTC ili null). " +
                        "Koristi samo ID-ove iz dostupnih šifrarnika u korisničkoj poruci."
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(context)
                }
            }
        };

        var responseText = await SendChatAsync(payload, cancellationToken);
        if (string.IsNullOrWhiteSpace(responseText))
            return await _fallback.SuggestCareTaskAsync(context, cancellationToken);

        try
        {
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;

            var residentId = root.TryGetProperty("residentId", out var residentIdElement)
                ? residentIdElement.GetInt32()
                : context.Residents.FirstOrDefault()?.Id ?? 0;

            var typeId = root.TryGetProperty("careTaskTypeId", out var typeIdElement)
                ? typeIdElement.GetInt32()
                : context.CareTaskTypes.FirstOrDefault()?.Id ?? 1;

            int? caregiverId = null;
            if (root.TryGetProperty("caregiverId", out var caregiverIdElement) &&
                caregiverIdElement.ValueKind != JsonValueKind.Null)
            {
                caregiverId = caregiverIdElement.GetInt32();
            }

            DateTime? dueAt = null;
            if (root.TryGetProperty("dueAt", out var dueAtElement) &&
                dueAtElement.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(dueAtElement.GetString(), out var parsedDueAt))
            {
                dueAt = parsedDueAt.ToUniversalTime();
            }

            return new AiCareTaskSuggestionDto
            {
                Title = root.TryGetProperty("title", out var titleElement)
                    ? titleElement.GetString() ?? "Novi zadatak"
                    : "Novi zadatak",
                Description = root.TryGetProperty("description", out var descriptionElement)
                    ? descriptionElement.GetString()
                    : context.Note,
                ResidentId = residentId,
                ResidentName = context.Residents.FirstOrDefault(item => item.Id == residentId)?.Name ?? string.Empty,
                CareTaskTypeId = typeId,
                TypeName = context.CareTaskTypes.FirstOrDefault(item => item.Id == typeId)?.Name ?? string.Empty,
                CaregiverId = caregiverId,
                CaregiverName = caregiverId.HasValue
                    ? context.Caregivers.FirstOrDefault(item => item.Id == caregiverId.Value)?.Name
                    : null,
                DueAt = dueAt
            };
        }
        catch (JsonException)
        {
            return await _fallback.SuggestCareTaskAsync(context, cancellationToken);
        }
    }

    private async Task<string?> SendChatAsync(object payload, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }
}
