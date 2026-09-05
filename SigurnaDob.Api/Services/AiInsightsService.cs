using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Api.Services.Ai;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Services;

public class AiInsightsService
{
    private readonly SigurnaDobDbContext _context;
    private readonly IAiService _aiService;

    public AiInsightsService(SigurnaDobDbContext context, IAiService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    public async Task<AiSummaryResponseDto> BuildSummaryAsync(CancellationToken cancellationToken = default)
    {
        var context = await BuildOperationalContextAsync(cancellationToken);
        var summary = await _aiService.GenerateOperationalSummaryAsync(context, cancellationToken);

        return new AiSummaryResponseDto
        {
            Summary = summary,
            Provider = _aiService.ProviderName
        };
    }

    public async Task<AiCareTaskSuggestionDto> SuggestCareTaskAsync(
        AiCareTaskSuggestionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Note))
            throw new InvalidOperationException("Bilješka je obavezna za AI prijedlog.");

        var context = new AiCareTaskSuggestionContextDto
        {
            Note = request.Note.Trim(),
            Residents = await _context.Residents
                .AsNoTracking()
                .Where(resident =>
                    resident.ResidentStatusId == ResidentStatusIds.Active ||
                    resident.ResidentStatusId == ResidentStatusIds.TemporarilyAbsent ||
                    resident.ResidentStatusId == ResidentStatusIds.Preparing)
                .OrderBy(resident => resident.LastName)
                .Select(resident => new LookupDto
                {
                    Id = resident.Id,
                    Name = resident.FirstName + " " + resident.LastName
                })
                .ToListAsync(cancellationToken),
            CareTaskTypes = await _context.CareTaskTypes
                .AsNoTracking()
                .OrderBy(type => type.Name)
                .Select(type => new LookupDto { Id = type.Id, Name = type.Name })
                .ToListAsync(cancellationToken),
            Caregivers = await _context.Employees
                .AsNoTracking()
                .Where(employee => employee.EmployeePositionId == 2 && employee.EmployeeStatusId == 1)
                .OrderBy(employee => employee.LastName)
                .Select(employee => new LookupDto
                {
                    Id = employee.Id,
                    Name = employee.FirstName + " " + employee.LastName
                })
                .ToListAsync(cancellationToken)
        };

        return await _aiService.SuggestCareTaskAsync(context, cancellationToken);
    }

    private async Task<AiOperationalContextDto> BuildOperationalContextAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var openTasks = await _context.CareTasks
            .AsNoTracking()
            .Include(task => task.Resident)
            .Include(task => task.CareTaskType)
            .Include(task => task.CareTaskStatus)
            .Include(task => task.Caregiver)
            .Where(task =>
                task.CareTaskStatusId != CareTaskStatusIds.Completed &&
                task.CareTaskStatusId != CareTaskStatusIds.Cancelled)
            .OrderBy(task => task.DueAt ?? DateTime.MaxValue)
            .Take(8)
            .ToListAsync(cancellationToken);

        var pendingVisits = await _context.VisitRequests
            .AsNoTracking()
            .Include(request => request.Resident)
            .Include(request => request.FamilyContact)
            .Include(request => request.VisitRequestStatus)
            .Where(request => request.VisitRequestStatusId == VisitRequestStatusIds.Received)
            .OrderBy(request => request.RequestedVisitAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        var pendingVisitCount = await _context.VisitRequests.CountAsync(
            request => request.VisitRequestStatusId == VisitRequestStatusIds.Received,
            cancellationToken);

        var openCount = await _context.CareTasks.CountAsync(task =>
            task.CareTaskStatusId != CareTaskStatusIds.Completed &&
            task.CareTaskStatusId != CareTaskStatusIds.Cancelled,
            cancellationToken);

        var overdueCount = await _context.CareTasks.CountAsync(task =>
            task.CareTaskStatusId != CareTaskStatusIds.Completed &&
            task.CareTaskStatusId != CareTaskStatusIds.Cancelled &&
            task.DueAt != null &&
            task.DueAt < utcNow,
            cancellationToken);

        return new AiOperationalContextDto
        {
            OpenCareTasks = openCount,
            OverdueCareTasks = overdueCount,
            PendingVisitRequests = pendingVisitCount,
            OpenTasks = openTasks.Select(task => new AiCareTaskSnapshotDto
            {
                Title = task.Title,
                ResidentName = $"{task.Resident?.FirstName} {task.Resident?.LastName}".Trim(),
                TypeName = task.CareTaskType?.Name ?? string.Empty,
                StatusName = task.CareTaskStatus?.Name ?? string.Empty,
                CaregiverName = task.Caregiver == null
                    ? null
                    : $"{task.Caregiver.FirstName} {task.Caregiver.LastName}".Trim(),
                DueAt = task.DueAt,
                IsOverdue = task.DueAt != null && task.DueAt < utcNow
            }).ToList(),
            PendingVisits = pendingVisits.Select(request => new AiVisitRequestSnapshotDto
            {
                ResidentName = $"{request.Resident?.FirstName} {request.Resident?.LastName}".Trim(),
                FamilyContactName =
                    $"{request.FamilyContact?.FirstName} {request.FamilyContact?.LastName}".Trim(),
                RequestedVisitAt = request.RequestedVisitAt,
                StatusName = request.VisitRequestStatus?.Name ?? string.Empty
            }).ToList()
        };
    }
}
