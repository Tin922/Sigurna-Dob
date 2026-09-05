namespace SigurnaDob.Shared.Models;

public class ResidentStatusHistory
{
    public int Id { get; set; }
    public int ResidentId { get; set; }
    public int? FromStatusId { get; set; }
    public int ToStatusId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public int? ChangedByAppUserId { get; set; }
    public string ChangedByName { get; set; } = string.Empty;

    public Resident? Resident { get; set; }
    public ResidentStatus? FromStatus { get; set; }
    public ResidentStatus? ToStatus { get; set; }
}
