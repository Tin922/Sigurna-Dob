namespace SigurnaDob.Shared.Dtos;

public class SaveCareTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ResidentId { get; set; }
    public int CareTaskTypeId { get; set; }
    public int CareTaskStatusId { get; set; }
    public int? CaregiverId { get; set; }
    public DateTime? DueAt { get; set; }
}
