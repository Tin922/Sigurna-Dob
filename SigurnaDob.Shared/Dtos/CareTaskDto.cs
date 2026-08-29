namespace SigurnaDob.Shared.Dtos;

public class CareTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ResidentId { get; set; }
    public string ResidentName { get; set; } = string.Empty;
    public int CareTaskTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public int CareTaskStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int? CaregiverId { get; set; }
    public string? CaregiverName { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsOverdue { get; set; }
}
