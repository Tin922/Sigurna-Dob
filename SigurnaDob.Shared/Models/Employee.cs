namespace SigurnaDob.Shared.Models;

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly? HiredAt { get; set; }
    public string? Note { get; set; }

    public int EmployeePositionId { get; set; }
    public EmployeePosition? EmployeePosition { get; set; }

    public int EmployeeStatusId { get; set; }
    public EmployeeStatus? EmployeeStatus { get; set; }

    public AppUser? AppUser { get; set; }

    public ICollection<CareTask> CoordinatedCareTasks { get; set; } = new List<CareTask>();
    public ICollection<CareTask> AssignedCareTasks { get; set; } = new List<CareTask>();
    public ICollection<VisitRequest> CoordinatedVisitRequests { get; set; } = new List<VisitRequest>();
    public ICollection<Activity> CoordinatedActivities { get; set; } = new List<Activity>();
}
