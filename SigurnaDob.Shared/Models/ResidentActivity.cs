namespace SigurnaDob.Shared.Models;

public class ResidentActivity
{
    public int ResidentId { get; set; }
    public Resident? Resident { get; set; }

    public int ActivityId { get; set; }
    public Activity? Activity { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}
