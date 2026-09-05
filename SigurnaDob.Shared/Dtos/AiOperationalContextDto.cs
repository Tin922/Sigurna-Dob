namespace SigurnaDob.Shared.Dtos;

public class AiOperationalContextDto
{
    public int OpenCareTasks { get; set; }
    public int OverdueCareTasks { get; set; }
    public int PendingVisitRequests { get; set; }
    public List<AiCareTaskSnapshotDto> OpenTasks { get; set; } = new();
    public List<AiVisitRequestSnapshotDto> PendingVisits { get; set; } = new();
}

public class AiCareTaskSnapshotDto
{
    public string Title { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? CaregiverName { get; set; }
    public DateTime? DueAt { get; set; }
    public bool IsOverdue { get; set; }
}

public class AiVisitRequestSnapshotDto
{
    public string ResidentName { get; set; } = string.Empty;
    public string FamilyContactName { get; set; } = string.Empty;
    public DateTime RequestedVisitAt { get; set; }
    public string StatusName { get; set; } = string.Empty;
}
