namespace SigurnaDob.Shared.Models;

public class ActivityType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }

    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
