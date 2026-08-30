using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Services;

public static class ResidentMediaBusinessRules
{
    private static readonly Dictionary<string, string[]> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = [".pdf"],
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"]
    };

    public static (ResidentMediaType MediaType, string NormalizedContentType)? ResolveAllowedFile(
        string fileName,
        string? contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        extension = extension.ToLowerInvariant();

        foreach (var (allowedContentType, extensions) in AllowedContentTypes)
        {
            if (!extensions.Contains(extension))
                continue;

            if (!string.IsNullOrWhiteSpace(contentType) &&
                !string.Equals(contentType, allowedContentType, StringComparison.OrdinalIgnoreCase) &&
                !IsCompatibleContentType(contentType, allowedContentType))
                continue;

            var mediaType = extension == ".pdf"
                ? ResidentMediaType.Document
                : ResidentMediaType.Image;

            return (mediaType, allowedContentType);
        }

        return null;
    }

    public static string? ValidateUpload(
        string fileName,
        string? contentType,
        long fileSizeBytes,
        long maxFileSizeBytes)
    {
        if (fileSizeBytes <= 0)
            return "Datoteka je prazna.";

        if (fileSizeBytes > maxFileSizeBytes)
            return $"Datoteka premašuje maksimalnu veličinu od {maxFileSizeBytes / (1024 * 1024)} MB.";

        if (ResolveAllowedFile(fileName, contentType) is null)
            return "Dopuštene su samo PDF, JPG i PNG datoteke.";

        return null;
    }

    private static bool IsCompatibleContentType(string contentType, string allowedContentType)
    {
        if (allowedContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        return string.Equals(contentType, allowedContentType, StringComparison.OrdinalIgnoreCase);
    }
}
