namespace SigurnaDob.Shared.Dtos;

public class FamilyContactDetailDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }
    public int ResidentId { get; set; }
    public string ResidentName { get; set; } = string.Empty;
    public bool HasLinkedAccount { get; set; }
    public int VisitRequestCount { get; set; }
}
