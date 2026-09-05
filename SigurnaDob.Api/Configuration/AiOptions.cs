namespace SigurnaDob.Api.Configuration;

public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Mock (default) or OpenAI.</summary>
    public string Provider { get; set; } = "Mock";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4o-mini";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}
