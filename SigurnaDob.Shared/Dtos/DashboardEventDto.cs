namespace SigurnaDob.Shared.Dtos;

public class DashboardEventDto
{
    public DateTime OccurredAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
