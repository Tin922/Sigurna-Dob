namespace SigurnaDob.Shared.Dtos;

public class RoomDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int RoomStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Occupancy { get; set; }
    public int AvailableSpots { get; set; }
    public string? Note { get; set; }
    public List<LookupDto> Residents { get; set; } = new();
}
