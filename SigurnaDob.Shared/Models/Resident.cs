namespace SigurnaDob.Shared.Models;

public class Resident
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? PersonalId { get; set; }
    public DateOnly? AdmittedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? Note { get; set; }

    public int ResidentStatusId { get; set; }
    public ResidentStatus? ResidentStatus { get; set; }

    public int? RoomId { get; set; }
    public Room? Room { get; set; }

    public ICollection<FamilyContact> FamilyContacts { get; set; } = new List<FamilyContact>();
    public ICollection<CareTask> CareTasks { get; set; } = new List<CareTask>();
    public ICollection<VisitRequest> VisitRequests { get; set; } = new List<VisitRequest>();
    public ICollection<ResidentActivity> ResidentActivities { get; set; } = new List<ResidentActivity>();
    public ICollection<ResidentMedia> Media { get; set; } = new List<ResidentMedia>();
}
