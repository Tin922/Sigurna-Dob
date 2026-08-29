namespace SigurnaDob.Shared.Dtos;

public class VisitRequestDto
{
    public int Id { get; set; }
    public int ResidentId { get; set; }
    public string ResidentName { get; set; } = string.Empty;
    public int FamilyContactId { get; set; }
    public string FamilyContactName { get; set; } = string.Empty;
    public int VisitRequestStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime RequestedVisitAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? HeldAt { get; set; }
    public string? DecisionNote { get; set; }
}
