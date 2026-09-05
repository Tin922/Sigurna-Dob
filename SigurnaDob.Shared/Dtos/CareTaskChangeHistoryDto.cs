namespace SigurnaDob.Shared.Dtos;

public class CareTaskChangeHistoryDto
{
    public int Id { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
}
