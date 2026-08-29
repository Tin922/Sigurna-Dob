namespace SigurnaDob.Shared.Dtos;

public class CompleteCareTaskDto
{
    public string CompletionNote { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
}
