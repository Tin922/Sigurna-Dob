namespace SigurnaDob.Shared.Dtos;

public class SaveAppUserDto
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> RoleIds { get; set; } = new();
    public int? EmployeeId { get; set; }
    public int? FamilyContactId { get; set; }
}
