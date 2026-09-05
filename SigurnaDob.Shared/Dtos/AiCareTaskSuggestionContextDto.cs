namespace SigurnaDob.Shared.Dtos;

public class AiCareTaskSuggestionContextDto
{
    public string Note { get; set; } = string.Empty;
    public List<LookupDto> Residents { get; set; } = new();
    public List<LookupDto> CareTaskTypes { get; set; } = new();
    public List<LookupDto> Caregivers { get; set; } = new();
}
