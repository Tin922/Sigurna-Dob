namespace SigurnaDob.Shared.Dtos;

public class RoomOccupancyItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RoomStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Occupancy { get; set; }
    public int AvailableSpots { get; set; }
    public int OccupancyPercent { get; set; }
    public bool IsFull { get; set; }
    public List<RoomOccupantDto> Occupants { get; set; } = new();
}
