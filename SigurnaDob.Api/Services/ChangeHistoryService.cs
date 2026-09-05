using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Services;

public class ChangeHistoryService
{
    private readonly SigurnaDobDbContext _context;

    public ChangeHistoryService(SigurnaDobDbContext context)
    {
        _context = context;
    }

    public void RecordResidentStatusChange(
        int residentId,
        int? fromStatusId,
        int toStatusId,
        ClaimsPrincipal user)
    {
        if (fromStatusId == toStatusId)
            return;

        var actor = ResolveActor(user);
        _context.ResidentStatusHistories.Add(new ResidentStatusHistory
        {
            ResidentId = residentId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            ChangedAt = DateTime.UtcNow,
            ChangedByAppUserId = actor.UserId,
            ChangedByName = actor.DisplayName
        });
    }

    public void RecordCareTaskStatusChange(
        int careTaskId,
        int? fromStatusId,
        int toStatusId,
        ClaimsPrincipal user)
    {
        if (fromStatusId == toStatusId)
            return;

        var actor = ResolveActor(user);
        _context.CareTaskChangeHistories.Add(new CareTaskChangeHistory
        {
            CareTaskId = careTaskId,
            ChangeType = CareTaskChangeTypes.Status,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            ChangedAt = DateTime.UtcNow,
            ChangedByAppUserId = actor.UserId,
            ChangedByName = actor.DisplayName
        });
    }

    public void RecordCareTaskAssignmentChange(
        int careTaskId,
        int? fromCaregiverId,
        int? toCaregiverId,
        ClaimsPrincipal user)
    {
        if (fromCaregiverId == toCaregiverId)
            return;

        var actor = ResolveActor(user);
        _context.CareTaskChangeHistories.Add(new CareTaskChangeHistory
        {
            CareTaskId = careTaskId,
            ChangeType = CareTaskChangeTypes.Assignment,
            FromCaregiverId = fromCaregiverId,
            ToCaregiverId = toCaregiverId,
            ChangedAt = DateTime.UtcNow,
            ChangedByAppUserId = actor.UserId,
            ChangedByName = actor.DisplayName
        });
    }

    public async Task<List<ResidentStatusHistoryDto>> GetResidentStatusHistoryAsync(int residentId)
    {
        return await _context.ResidentStatusHistories
            .AsNoTracking()
            .Include(entry => entry.FromStatus)
            .Include(entry => entry.ToStatus)
            .Where(entry => entry.ResidentId == residentId)
            .OrderByDescending(entry => entry.ChangedAt)
            .Select(entry => new ResidentStatusHistoryDto
            {
                Id = entry.Id,
                ChangedAt = entry.ChangedAt,
                FromStatusName = entry.FromStatus != null ? entry.FromStatus.Name : null,
                ToStatusName = entry.ToStatus != null ? entry.ToStatus.Name : string.Empty,
                ChangedByName = entry.ChangedByName
            })
            .ToListAsync();
    }

    public async Task<List<CareTaskChangeHistoryDto>> GetCareTaskHistoryAsync(int careTaskId)
    {
        var entries = await _context.CareTaskChangeHistories
            .AsNoTracking()
            .Include(entry => entry.FromStatus)
            .Include(entry => entry.ToStatus)
            .Include(entry => entry.FromCaregiver)
            .Include(entry => entry.ToCaregiver)
            .Where(entry => entry.CareTaskId == careTaskId)
            .OrderByDescending(entry => entry.ChangedAt)
            .ToListAsync();

        return entries.Select(ToCareTaskHistoryDto).ToList();
    }

    private static CareTaskChangeHistoryDto ToCareTaskHistoryDto(CareTaskChangeHistory entry)
    {
        var description = entry.ChangeType switch
        {
            CareTaskChangeTypes.Status =>
                $"{FormatStatus(entry.FromStatus?.Name)} → {FormatStatus(entry.ToStatus?.Name)}",
            CareTaskChangeTypes.Assignment =>
                $"{FormatCaregiver(entry.FromCaregiver)} → {FormatCaregiver(entry.ToCaregiver)}",
            _ => "Promjena"
        };

        var changeTypeLabel = entry.ChangeType switch
        {
            CareTaskChangeTypes.Status => "Status",
            CareTaskChangeTypes.Assignment => "Dodjela",
            _ => entry.ChangeType
        };

        return new CareTaskChangeHistoryDto
        {
            Id = entry.Id,
            ChangedAt = entry.ChangedAt,
            ChangeType = changeTypeLabel,
            Description = description,
            ChangedByName = entry.ChangedByName
        };
    }

    private static string FormatStatus(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "—" : name;

    private static string FormatCaregiver(Employee? employee) =>
        employee is null
            ? "Nije dodijeljen"
            : $"{employee.FirstName} {employee.LastName}".Trim();

    private static (int? UserId, string DisplayName) ResolveActor(ClaimsPrincipal user)
    {
        int? userId = int.TryParse(
            user.FindFirstValue(ClaimTypes.NameIdentifier),
            out var parsedUserId)
            ? parsedUserId
            : null;

        var displayName = user.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = user.FindFirstValue(ClaimTypes.Email) ?? "Sustav";

        return (userId, displayName);
    }
}
