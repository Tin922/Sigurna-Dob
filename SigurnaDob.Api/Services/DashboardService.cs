using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Api.Security;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Services;

public class DashboardService
{
    private readonly SigurnaDobDbContext _context;

    public DashboardService(SigurnaDobDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> BuildAsync(ClaimsPrincipal user)
    {
        var utcNow = DateTime.UtcNow;
        var isAdminOrCoordinator = user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.Coordinator);
        var isCaregiver = user.IsInRole(AppRoles.Caregiver);
        var isFamily = user.IsInRole(AppRoles.FamilyMember);

        var dashboard = new DashboardDto();

        if (isAdminOrCoordinator)
            dashboard.Operational = await BuildOperationalAsync(utcNow);

        dashboard.Personal = await BuildPersonalAsync(user, isCaregiver, isFamily);
        dashboard.RecentEvents = await BuildRecentEventsAsync(user, isAdminOrCoordinator, isFamily, utcNow);

        return dashboard;
    }

    private async Task<DashboardOperationalDto> BuildOperationalAsync(DateTime utcNow)
    {
        var activeResidents = await _context.Residents.CountAsync(resident =>
            resident.ResidentStatusId == ResidentStatusIds.Active ||
            resident.ResidentStatusId == ResidentStatusIds.TemporarilyAbsent);

        var rooms = await _context.Rooms
            .AsNoTracking()
            .Where(room => room.RoomStatusId == RoomStatusIds.InUse)
            .Select(room => new { room.Id, room.Capacity })
            .ToListAsync();

        var roomIds = rooms.Select(room => room.Id).ToList();
        var occupancyByRoom = roomIds.Count == 0
            ? new Dictionary<int, int>()
            : await _context.Residents
                .AsNoTracking()
                .Where(resident =>
                    resident.RoomId != null &&
                    roomIds.Contains(resident.RoomId.Value) &&
                    resident.ResidentStatusId != ResidentStatusIds.MovedOut &&
                    resident.ResidentStatusId != ResidentStatusIds.Archived)
                .GroupBy(resident => resident.RoomId!.Value)
                .Select(group => new { RoomId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.RoomId, item => item.Count);

        var availableSpots = rooms.Sum(room =>
        {
            var occupancy = occupancyByRoom.GetValueOrDefault(room.Id);
            return Math.Max(0, room.Capacity - occupancy);
        });

        var openCareTasks = await _context.CareTasks.CountAsync(task =>
            task.CareTaskStatusId != CareTaskStatusIds.Completed &&
            task.CareTaskStatusId != CareTaskStatusIds.Cancelled);

        var overdueCareTasks = await _context.CareTasks.CountAsync(task =>
            task.CareTaskStatusId != CareTaskStatusIds.Completed &&
            task.CareTaskStatusId != CareTaskStatusIds.Cancelled &&
            task.DueAt != null &&
            task.DueAt < utcNow);

        var pendingVisitRequests = await _context.VisitRequests.CountAsync(request =>
            request.VisitRequestStatusId == VisitRequestStatusIds.Received);

        return new DashboardOperationalDto
        {
            ActiveResidents = activeResidents,
            AvailableSpots = availableSpots,
            OpenCareTasks = openCareTasks,
            OverdueCareTasks = overdueCareTasks,
            PendingVisitRequests = pendingVisitRequests
        };
    }

    private async Task<DashboardPersonalDto?> BuildPersonalAsync(
        ClaimsPrincipal user,
        bool isCaregiver,
        bool isFamily)
    {
        if (!isCaregiver && !isFamily)
            return null;

        var personal = new DashboardPersonalDto();

        if (isCaregiver)
        {
            var employeeId = GetEmployeeId(user);
            if (employeeId.HasValue)
            {
                personal.MyOpenCareTasks = await _context.CareTasks.CountAsync(task =>
                    task.CaregiverId == employeeId.Value &&
                    task.CareTaskStatusId != CareTaskStatusIds.Completed &&
                    task.CareTaskStatusId != CareTaskStatusIds.Cancelled);
            }
        }

        if (isFamily)
        {
            var familyContactId = GetFamilyContactId(user);
            if (familyContactId.HasValue)
            {
                personal.MyVisitRequests = await _context.VisitRequests.CountAsync(request =>
                    request.FamilyContactId == familyContactId.Value);
            }
        }

        return personal;
    }

    private async Task<List<DashboardEventDto>> BuildRecentEventsAsync(
        ClaimsPrincipal user,
        bool isAdminOrCoordinator,
        bool isFamily,
        DateTime utcNow)
    {
        var events = new List<DashboardEventDto>();
        var familyContactId = isFamily && !isAdminOrCoordinator
            ? GetFamilyContactId(user)
            : null;

        if (familyContactId.HasValue)
        {
            var visitEvents = await _context.VisitRequests
                .AsNoTracking()
                .Include(request => request.Resident)
                .Include(request => request.VisitRequestStatus)
                .Where(request => request.FamilyContactId == familyContactId.Value)
                .OrderByDescending(request => request.UpdatedAt)
                .Take(5)
                .ToListAsync();

            events.AddRange(visitEvents.Select(request => new DashboardEventDto
            {
                OccurredAt = request.UpdatedAt,
                Title = $"Zahtjev za posjet — {request.VisitRequestStatus?.Name}",
                Description =
                    $"Termin {request.RequestedVisitAt.ToLocalTime():dd.MM.yyyy HH:mm} za " +
                    $"{request.Resident?.FirstName} {request.Resident?.LastName}".Trim()
            }));

            return events
                .OrderByDescending(item => item.OccurredAt)
                .Take(5)
                .ToList();
        }

        var completedTasks = await _context.CareTasks
            .AsNoTracking()
            .Include(task => task.Resident)
            .Where(task => task.CompletedAt != null)
            .OrderByDescending(task => task.CompletedAt)
            .Take(5)
            .ToListAsync();

        events.AddRange(completedTasks.Select(task => new DashboardEventDto
        {
            OccurredAt = task.CompletedAt!.Value,
            Title = "Zadatak izvršen",
            Description = $"{task.Title} — {task.Resident?.FirstName} {task.Resident?.LastName}".Trim()
        }));

        var newTasks = await _context.CareTasks
            .AsNoTracking()
            .Include(task => task.Resident)
            .OrderByDescending(task => task.CreatedAt)
            .Take(5)
            .ToListAsync();

        events.AddRange(newTasks.Select(task => new DashboardEventDto
        {
            OccurredAt = task.CreatedAt,
            Title = "Novi zadatak skrbi",
            Description = $"{task.Title} — {task.Resident?.FirstName} {task.Resident?.LastName}".Trim()
        }));

        var visitUpdates = await _context.VisitRequests
            .AsNoTracking()
            .Include(request => request.Resident)
            .Include(request => request.VisitRequestStatus)
            .Where(request =>
                request.DecidedAt != null ||
                request.VisitRequestStatusId == VisitRequestStatusIds.Received)
            .OrderByDescending(request => request.UpdatedAt)
            .Take(5)
            .ToListAsync();

        events.AddRange(visitUpdates.Select(request => new DashboardEventDto
        {
            OccurredAt = request.DecidedAt ?? request.RequestedAt,
            Title = request.DecidedAt.HasValue
                ? $"Posjet {request.VisitRequestStatus?.Name?.ToLower()}"
                : "Novi zahtjev za posjet",
            Description =
                $"{request.Resident?.FirstName} {request.Resident?.LastName} — " +
                $"{request.RequestedVisitAt.ToLocalTime():dd.MM.yyyy HH:mm}"
        }));

        var recentActivities = await _context.Activities
            .AsNoTracking()
            .OrderByDescending(activity => activity.UpdatedAt)
            .Take(3)
            .ToListAsync();

        events.AddRange(recentActivities.Select(activity => new DashboardEventDto
        {
            OccurredAt = activity.UpdatedAt,
            Title = "Aktivnost ažurirana",
            Description = $"{activity.Title} — {activity.StartsAt.ToLocalTime():dd.MM.yyyy HH:mm}"
        }));

        return events
            .OrderByDescending(item => item.OccurredAt)
            .Take(5)
            .ToList();
    }

    private static int? GetEmployeeId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(AppClaimTypes.EmployeeId)?.Value
                    ?? user.FindFirstValue(AppClaimTypes.EmployeeId);

        return int.TryParse(claim, out var employeeId) ? employeeId : null;
    }

    private static int? GetFamilyContactId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(AppClaimTypes.FamilyContactId)?.Value
                    ?? user.FindFirstValue(AppClaimTypes.FamilyContactId);

        return int.TryParse(claim, out var familyContactId) ? familyContactId : null;
    }
}
