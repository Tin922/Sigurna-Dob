namespace SigurnaDob.Shared.Dtos;

public class RoomOccupancySummaryDto
{
    public int TotalRooms { get; set; }
    public int RoomsInUse { get; set; }
    public int TotalCapacity { get; set; }
    public int TotalOccupancy { get; set; }
    public int AvailableSpots { get; set; }
    public int FullRooms { get; set; }
    public int EmptyRooms { get; set; }
}
