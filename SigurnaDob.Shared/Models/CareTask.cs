namespace SigurnaDob.Shared.Models;

public class CareTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletionNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int ResidentId { get; set; }
    public Resident? Resident { get; set; }

    public int CareTaskTypeId { get; set; }
    public CareTaskType? CareTaskType { get; set; }

    public int CareTaskStatusId { get; set; }
    public CareTaskStatus? CareTaskStatus { get; set; }

    public int CoordinatorId { get; set; }
    public Employee? Coordinator { get; set; }

    public int? CaregiverId { get; set; }
    public Employee? Caregiver { get; set; }
}
