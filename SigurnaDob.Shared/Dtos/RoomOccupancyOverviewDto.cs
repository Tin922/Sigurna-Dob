namespace SigurnaDob.Shared.Dtos;

public class RoomOccupancyOverviewDto
{
    public RoomOccupancySummaryDto Summary { get; set; } = new();
    public List<RoomOccupancyItemDto> Rooms { get; set; } = new();
}
