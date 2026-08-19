namespace SigurnaDob.Shared.Models;

public enum ResidentMediaType
{
    Document = 1,
    Image = 2
}

public class ResidentMedia
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public ResidentMediaType MediaType { get; set; }
    public string? Caption { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int ResidentId { get; set; }
    public Resident? Resident { get; set; }
}
