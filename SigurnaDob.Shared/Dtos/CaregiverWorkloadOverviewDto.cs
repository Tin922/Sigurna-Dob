namespace SigurnaDob.Shared.Dtos;

public class CaregiverWorkloadOverviewDto
{
    public CaregiverWorkloadSummaryDto Summary { get; set; } = new();
    public List<CaregiverWorkloadItemDto> Caregivers { get; set; } = new();
    public List<CaregiverWorkloadTaskDto> UnassignedTasks { get; set; } = new();
}
