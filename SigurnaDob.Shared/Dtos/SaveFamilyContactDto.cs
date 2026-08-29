namespace SigurnaDob.Shared.Dtos;

public class SaveFamilyContactDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }
    public int ResidentId { get; set; }
}
