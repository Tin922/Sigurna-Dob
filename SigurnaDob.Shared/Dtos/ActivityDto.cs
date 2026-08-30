namespace SigurnaDob.Shared.Dtos;

public class ActivityDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ActivityTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
    public int ParticipantCount { get; set; }
    public string? CoordinatorName { get; set; }
}
