namespace SigurnaDob.Shared.Models;

public class Activity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int ActivityTypeId { get; set; }
    public ActivityType? ActivityType { get; set; }

    public int? CoordinatorId { get; set; }
    public Employee? Coordinator { get; set; }

    public ICollection<ResidentActivity> ResidentActivities { get; set; } = new List<ResidentActivity>();
}
