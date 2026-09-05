namespace SigurnaDob.Shared.Dtos;

public class ResidentStatusHistoryDto
{
    public int Id { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? FromStatusName { get; set; }
    public string ToStatusName { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
}
