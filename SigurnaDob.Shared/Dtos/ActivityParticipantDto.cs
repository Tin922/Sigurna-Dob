namespace SigurnaDob.Shared.Dtos;

public class ActivityParticipantDto
{
    public int ResidentId { get; set; }
    public string ResidentName { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public string? Note { get; set; }
}
