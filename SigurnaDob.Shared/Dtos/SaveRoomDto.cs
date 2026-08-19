namespace SigurnaDob.Shared.Dtos;

public class SaveRoomDto
{
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int RoomStatusId { get; set; }
    public string? Note { get; set; }
}
