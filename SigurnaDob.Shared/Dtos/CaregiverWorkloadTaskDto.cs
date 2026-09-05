namespace SigurnaDob.Shared.Dtos;

public class CaregiverWorkloadTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public DateTime? DueAt { get; set; }
    public bool IsOverdue { get; set; }
}
