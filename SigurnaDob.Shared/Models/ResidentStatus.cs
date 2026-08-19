namespace SigurnaDob.Shared.Models;

public class ResidentStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }

    public ICollection<Resident> Residents { get; set; } = new List<Resident>();
}
