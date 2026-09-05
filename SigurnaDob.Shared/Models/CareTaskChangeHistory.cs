namespace SigurnaDob.Shared.Models;

public class CareTaskChangeHistory
{
    public int Id { get; set; }
    public int CareTaskId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public int? FromStatusId { get; set; }
    public int? ToStatusId { get; set; }
    public int? FromCaregiverId { get; set; }
    public int? ToCaregiverId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public int? ChangedByAppUserId { get; set; }
    public string ChangedByName { get; set; } = string.Empty;

    public CareTask? CareTask { get; set; }
    public CareTaskStatus? FromStatus { get; set; }
    public CareTaskStatus? ToStatus { get; set; }
    public Employee? FromCaregiver { get; set; }
    public Employee? ToCaregiver { get; set; }
}
