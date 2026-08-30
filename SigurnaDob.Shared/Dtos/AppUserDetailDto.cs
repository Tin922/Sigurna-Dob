namespace SigurnaDob.Shared.Dtos;

public class AppUserDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int> RoleIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int? FamilyContactId { get; set; }
    public string? FamilyContactName { get; set; }
}
