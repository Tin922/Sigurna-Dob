namespace SigurnaDob.Shared.Dtos;

public class SaveResidentDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? PersonalId { get; set; }
    public int ResidentStatusId { get; set; }
    public int? RoomId { get; set; }
    public DateOnly? AdmittedAt { get; set; }
    public string? Note { get; set; }
}
