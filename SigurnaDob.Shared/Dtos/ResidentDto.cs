namespace SigurnaDob.Shared.Dtos;

public class ResidentDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public int ResidentStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
    public DateOnly? AdmittedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
}
