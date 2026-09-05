namespace SigurnaDob.Shared.Dtos;

public class CaregiverWorkloadSummaryDto
{
    public int ActiveCaregivers { get; set; }
    public int TotalOpenTasks { get; set; }
    public int UnassignedOpenTasks { get; set; }
    public int TotalOverdueTasks { get; set; }
    public int HighLoadCaregivers { get; set; }
}
