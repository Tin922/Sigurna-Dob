using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SigurnaDob.Api.Configuration;
using SigurnaDob.Api.Data;
using SigurnaDob.Api.Security;
using SigurnaDob.Api.Services;
using SigurnaDob.Shared.Dtos;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiController]
[Route("api/residents/{residentId:int}/media")]
public class ResidentMediaController : ControllerBase
{
    private readonly SigurnaDobDbContext _context;
    private readonly ResidentMediaStorageService _storage;
    private readonly FileUploadOptions _uploadOptions;

    public ResidentMediaController(
        SigurnaDobDbContext context,
        ResidentMediaStorageService storage,
        IOptions<FileUploadOptions> uploadOptions)
    {
        _context = context;
        _storage = storage;
        _uploadOptions = uploadOptions.Value;
    }

    [HttpGet]
    public async Task<ActionResult<List<ResidentMediaDto>>> GetResidentMedia(int residentId)
    {
        if (!await ResidentExistsAsync(residentId))
            return NotFound();

        var media = await _context.ResidentMedia
            .AsNoTracking()
            .Where(item => item.ResidentId == residentId)
            .OrderByDescending(item => item.UploadedAt)
            .ToListAsync();

        return Ok(media.Select(ToDto).ToList());
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<ResidentMediaDto>> UploadResidentMedia(
        int residentId,
        IFormFile file,
        [FromForm] string? caption)
    {
        if (!await ResidentExistsAsync(residentId))
            return NotFound();

        if (file.Length == 0)
            return BadRequest("Datoteka nije poslana.");

        var validationError = ResidentMediaBusinessRules.ValidateUpload(
            file.FileName,
            file.ContentType,
            file.Length,
            _uploadOptions.MaxFileSizeBytes);

        if (validationError is not null)
            return BadRequest(validationError);

        var resolved = ResidentMediaBusinessRules.ResolveAllowedFile(file.FileName, file.ContentType);
        if (resolved is null)
            return BadRequest("Dopuštene su samo PDF, JPG i PNG datoteke.");

        var (mediaType, normalizedContentType) = resolved.Value;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        await using var stream = file.OpenReadStream();
        var (storedFileName, storedPath) = await _storage.SaveAsync(residentId, stream, extension);

        var media = new ResidentMedia
        {
            ResidentId = residentId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            StoredPath = storedPath,
            ContentType = normalizedContentType,
            FileSizeBytes = file.Length,
            MediaType = mediaType,
            Caption = NormalizeOptional(caption),
            UploadedAt = DateTime.UtcNow
        };

        _context.ResidentMedia.Add(media);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(DownloadResidentMedia),
            new { residentId, mediaId = media.Id },
            ToDto(media));
    }

    [HttpGet("{mediaId:int}/download")]
    public async Task<IActionResult> DownloadResidentMedia(int residentId, int mediaId)
    {
        var media = await _context.ResidentMedia
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == mediaId && item.ResidentId == residentId);

        if (media is null)
            return NotFound();

        var absolutePath = _storage.GetAbsolutePath(media.StoredPath);
        if (!System.IO.File.Exists(absolutePath))
            return NotFound("Fizička datoteka nije pronađena.");

        return PhysicalFile(absolutePath, media.ContentType, media.OriginalFileName);
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpDelete("{mediaId:int}")]
    public async Task<IActionResult> DeleteResidentMedia(int residentId, int mediaId)
    {
        var media = await _context.ResidentMedia
            .FirstOrDefaultAsync(item => item.Id == mediaId && item.ResidentId == residentId);

        if (media is null)
            return NotFound();

        var storedPath = media.StoredPath;
        _context.ResidentMedia.Remove(media);
        await _context.SaveChangesAsync();

        _storage.DeleteIfExists(storedPath);
        return NoContent();
    }

    private Task<bool> ResidentExistsAsync(int residentId) =>
        _context.Residents.AnyAsync(resident => resident.Id == residentId);

    private static ResidentMediaDto ToDto(ResidentMedia media) =>
        new()
        {
            Id = media.Id,
            ResidentId = media.ResidentId,
            OriginalFileName = media.OriginalFileName,
            ContentType = media.ContentType,
            FileSizeBytes = media.FileSizeBytes,
            MediaTypeId = (int)media.MediaType,
            MediaTypeName = media.MediaType == ResidentMediaType.Document ? "Dokument" : "Slika",
            Caption = media.Caption,
            UploadedAt = media.UploadedAt
        };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
