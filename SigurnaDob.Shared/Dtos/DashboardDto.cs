namespace SigurnaDob.Shared.Dtos;

public class DashboardDto
{
    public DashboardOperationalDto? Operational { get; set; }
    public DashboardPersonalDto? Personal { get; set; }
    public List<DashboardEventDto> RecentEvents { get; set; } = new();
}
