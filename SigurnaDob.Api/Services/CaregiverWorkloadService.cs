using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Api.Security;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Services;

public class CaregiverWorkloadService
{
    private const int HighLoadOpenTasks = 3;
    private const int WorkloadScaleMaxTasks = 5;

    private readonly SigurnaDobDbContext _context;

    public CaregiverWorkloadService(SigurnaDobDbContext context)
    {
        _context = context;
    }

    public async Task<CaregiverWorkloadOverviewDto> GetOverviewAsync(ClaimsPrincipal user)
    {
        var utcNow = DateTime.UtcNow;
        var isAdminOrCoordinator =
            user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.Coordinator);
        var isCaregiver = user.IsInRole(AppRoles.Caregiver);
        var currentEmployeeId = GetEmployeeId(user);

        var caregiversQuery = _context.Employees
            .AsNoTracking()
            .Where(employee =>
                employee.EmployeePositionId == 2 &&
                employee.EmployeeStatusId == 1);

        if (isCaregiver && !isAdminOrCoordinator)
        {
            if (currentEmployeeId is null)
                return new CaregiverWorkloadOverviewDto();

            caregiversQuery = caregiversQuery.Where(employee => employee.Id == currentEmployeeId.Value);
        }

        var caregivers = await caregiversQuery
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ToListAsync();

        var openTasks = await _context.CareTasks
            .AsNoTracking()
            .Include(task => task.Resident)
            .Include(task => task.CareTaskStatus)
            .Where(task =>
                task.CareTaskStatusId != CareTaskStatusIds.Completed &&
                task.CareTaskStatusId != CareTaskStatusIds.Cancelled)
            .ToListAsync();

        if (isCaregiver && !isAdminOrCoordinator && currentEmployeeId.HasValue)
        {
            openTasks = openTasks
                .Where(task => task.CaregiverId == currentEmployeeId.Value)
                .ToList();
        }

        var tasksByCaregiver = openTasks
            .Where(task => task.CaregiverId.HasValue)
            .GroupBy(task => task.CaregiverId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var caregiverItems = caregivers
            .Select(caregiver =>
            {
                var tasks = tasksByCaregiver.GetValueOrDefault(caregiver.Id) ?? new List<CareTask>();
                return BuildCaregiverItem(caregiver, tasks, utcNow);
            })
            .OrderByDescending(item => item.OpenTasks)
            .ThenByDescending(item => item.OverdueTasks)
            .ThenBy(item => item.Name)
            .ToList();

        var unassignedTasks = isAdminOrCoordinator
            ? openTasks
                .Where(task => task.CaregiverId is null)
                .Select(task => ToTaskDto(task, utcNow))
                .OrderBy(task => task.DueAt ?? DateTime.MaxValue)
                .ThenBy(task => task.Title)
                .ToList()
            : new List<CaregiverWorkloadTaskDto>();

        var summaryOpenTasks = openTasks;
        var highLoadCaregivers = caregiverItems.Count(item => item.WorkloadLevel == "high");

        return new CaregiverWorkloadOverviewDto
        {
            Summary = new CaregiverWorkloadSummaryDto
            {
                ActiveCaregivers = caregiverItems.Count,
                TotalOpenTasks = summaryOpenTasks.Count,
                UnassignedOpenTasks = unassignedTasks.Count,
                TotalOverdueTasks = summaryOpenTasks.Count(task => CareTaskBusinessRules.IsOverdue(task, utcNow)),
                HighLoadCaregivers = highLoadCaregivers
            },
            Caregivers = caregiverItems,
            UnassignedTasks = unassignedTasks
        };
    }

    private static CaregiverWorkloadItemDto BuildCaregiverItem(
        Employee caregiver,
        List<CareTask> tasks,
        DateTime utcNow)
    {
        var openTasks = tasks.Count;
        var overdueTasks = tasks.Count(task => CareTaskBusinessRules.IsOverdue(task, utcNow));
        var workloadLevel = ResolveWorkloadLevel(openTasks, overdueTasks);
        var percent = openTasks <= 0
            ? 0
            : (int)Math.Min(100, Math.Round(openTasks * 100.0 / WorkloadScaleMaxTasks));

        return new CaregiverWorkloadItemDto
        {
            EmployeeId = caregiver.Id,
            Name = $"{caregiver.FirstName} {caregiver.LastName}",
            OpenTasks = openTasks,
            AssignedTasks = tasks.Count(task => task.CareTaskStatusId == CareTaskStatusIds.Assigned),
            InProgressTasks = tasks.Count(task => task.CareTaskStatusId == CareTaskStatusIds.InProgress),
            OverdueTasks = overdueTasks,
            WorkloadPercent = percent,
            WorkloadLevel = workloadLevel,
            Tasks = tasks
                .Select(task => ToTaskDto(task, utcNow))
                .OrderBy(task => task.DueAt ?? DateTime.MaxValue)
                .ThenBy(task => task.Title)
                .ToList()
        };
    }

    private static string ResolveWorkloadLevel(int openTasks, int overdueTasks)
    {
        if (openTasks >= HighLoadOpenTasks || overdueTasks >= 2)
            return "high";

        if (openTasks >= 2 || overdueTasks >= 1)
            return "medium";

        return "low";
    }

    private static CaregiverWorkloadTaskDto ToTaskDto(CareTask task, DateTime utcNow) =>
        new()
        {
            Id = task.Id,
            Title = task.Title,
            StatusName = task.CareTaskStatus?.Name ?? string.Empty,
            ResidentName = task.Resident != null
                ? $"{task.Resident.FirstName} {task.Resident.LastName}"
                : string.Empty,
            DueAt = task.DueAt,
            IsOverdue = CareTaskBusinessRules.IsOverdue(task, utcNow)
        };

    private static int? GetEmployeeId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(AppClaimTypes.EmployeeId)?.Value
                    ?? user.FindFirstValue(AppClaimTypes.EmployeeId);

        return int.TryParse(claim, out var employeeId) ? employeeId : null;
    }
}
