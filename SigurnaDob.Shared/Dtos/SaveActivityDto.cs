namespace SigurnaDob.Shared.Dtos;

public class SaveActivityDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
    public int ActivityTypeId { get; set; }
}
