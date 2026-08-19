namespace SigurnaDob.Shared.Models;

public class EmployeePosition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
