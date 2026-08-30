namespace SigurnaDob.Shared.Dtos;

public class DashboardOperationalDto
{
    public int ActiveResidents { get; set; }
    public int AvailableSpots { get; set; }
    public int OpenCareTasks { get; set; }
    public int OverdueCareTasks { get; set; }
    public int PendingVisitRequests { get; set; }
}
