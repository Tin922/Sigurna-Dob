using Microsoft.Extensions.Options;
using SigurnaDob.Api.Configuration;

namespace SigurnaDob.Api.Services;

public class ResidentMediaStorageService
{
    private readonly string _rootFolder;
    private readonly IWebHostEnvironment _environment;

    public ResidentMediaStorageService(
        IWebHostEnvironment environment,
        IOptions<FileUploadOptions> options)
    {
        _environment = environment;
        _rootFolder = options.Value.RootFolder.Trim().Trim('/');
    }

    public string GetAbsolutePath(string storedPath) =>
        Path.Combine(_environment.ContentRootPath, storedPath.Replace('/', Path.DirectorySeparatorChar));

    public async Task<(string StoredFileName, string StoredPath)> SaveAsync(
        int residentId,
        Stream content,
        string extension)
    {
        var residentFolder = Path.Combine(
            _environment.ContentRootPath,
            _rootFolder,
            residentId.ToString());

        Directory.CreateDirectory(residentFolder);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(residentFolder, storedFileName);
        var storedPath = Path.Combine(_rootFolder, residentId.ToString(), storedFileName)
            .Replace('\\', '/');

        await using var fileStream = new FileStream(
            absolutePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await content.CopyToAsync(fileStream);

        return (storedFileName, storedPath);
    }

    public void DeleteIfExists(string storedPath)
    {
        var absolutePath = GetAbsolutePath(storedPath);
        if (File.Exists(absolutePath))
            File.Delete(absolutePath);
    }
}
