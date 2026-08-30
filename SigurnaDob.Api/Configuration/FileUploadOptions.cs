namespace SigurnaDob.Api.Configuration;

public class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    public string RootFolder { get; set; } = "uploads/residents";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}
