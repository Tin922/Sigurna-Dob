namespace SigurnaDob.Shared.Dtos;

public class AiCareTaskSuggestionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ResidentId { get; set; }
    public string ResidentName { get; set; } = string.Empty;
    public int CareTaskTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public int? CaregiverId { get; set; }
    public string? CaregiverName { get; set; }
    public DateTime? DueAt { get; set; }
}
