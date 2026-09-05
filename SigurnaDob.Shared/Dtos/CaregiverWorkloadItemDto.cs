namespace SigurnaDob.Shared.Dtos;

public class CaregiverWorkloadItemDto
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OpenTasks { get; set; }
    public int AssignedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int WorkloadPercent { get; set; }
    public string WorkloadLevel { get; set; } = "low";
    public List<CaregiverWorkloadTaskDto> Tasks { get; set; } = new();
}
