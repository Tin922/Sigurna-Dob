namespace SigurnaDob.Shared.Models;

public class VisitRequest
{
    public int Id { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime RequestedVisitAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? HeldAt { get; set; }
    public string? DecisionNote { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int ResidentId { get; set; }
    public Resident? Resident { get; set; }

    public int FamilyContactId { get; set; }
    public FamilyContact? FamilyContact { get; set; }

    public int? CoordinatorId { get; set; }
    public Employee? Coordinator { get; set; }

    public int VisitRequestStatusId { get; set; }
    public VisitRequestStatus? VisitRequestStatus { get; set; }
}
