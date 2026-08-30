namespace SigurnaDob.Shared.Dtos;

public class ResidentMediaDto
{
    public int Id { get; set; }
    public int ResidentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int MediaTypeId { get; set; }
    public string MediaTypeName { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTime UploadedAt { get; set; }
}
