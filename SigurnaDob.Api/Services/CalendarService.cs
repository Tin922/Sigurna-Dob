using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Api.Security;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Services;

public class CalendarService
{
    private readonly SigurnaDobDbContext _context;

    public CalendarService(SigurnaDobDbContext context)
    {
        _context = context;
    }

    public async Task<List<CalendarEventDto>> GetEventsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        ClaimsPrincipal user)
    {
        if (toUtc < fromUtc)
            return new List<CalendarEventDto>();

        var isFamilyOnly = user.IsInRole(AppRoles.FamilyMember) &&
                           !user.IsInRole(AppRoles.Admin) &&
                           !user.IsInRole(AppRoles.Coordinator) &&
                           !user.IsInRole(AppRoles.Caregiver);

        var visitEvents = isFamilyOnly
            ? await GetFamilyVisitEventsAsync(fromUtc, toUtc, user)
            : await GetStaffVisitEventsAsync(fromUtc, toUtc);

        if (isFamilyOnly)
            return visitEvents;

        var activityEvents = await GetActivityEventsAsync(fromUtc, toUtc);
        return visitEvents
            .Concat(activityEvents)
            .OrderBy(item => item.StartsAt)
            .ToList();
    }

    private async Task<List<CalendarEventDto>> GetStaffVisitEventsAsync(
        DateTime fromUtc,
        DateTime toUtc)
    {
        return await _context.VisitRequests
            .AsNoTracking()
            .Include(request => request.Resident)
            .Include(request => request.FamilyContact)
            .Include(request => request.VisitRequestStatus)
            .Where(request =>
                request.RequestedVisitAt >= fromUtc &&
                request.RequestedVisitAt <= toUtc &&
                request.VisitRequestStatusId != VisitRequestStatusIds.Rejected &&
                request.VisitRequestStatusId != VisitRequestStatusIds.Cancelled)
            .Select(request => new CalendarEventDto
            {
                Id = request.Id,
                EventType = CalendarEventTypes.Visit,
                Title = $"Posjet — {request.Resident!.FirstName} {request.Resident.LastName}",
                Subtitle = request.FamilyContact != null
                    ? $"{request.FamilyContact.FirstName} {request.FamilyContact.LastName}"
                    : null,
                StatusName = request.VisitRequestStatus!.Name,
                StartsAt = request.RequestedVisitAt,
                EndsAt = null
            })
            .ToListAsync();
    }

    private async Task<List<CalendarEventDto>> GetFamilyVisitEventsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        ClaimsPrincipal user)
    {
        var familyContactId = GetFamilyContactId(user);
        if (familyContactId is null)
            return new List<CalendarEventDto>();

        return await _context.VisitRequests
            .AsNoTracking()
            .Include(request => request.Resident)
            .Include(request => request.VisitRequestStatus)
            .Where(request =>
                request.FamilyContactId == familyContactId.Value &&
                request.RequestedVisitAt >= fromUtc &&
                request.RequestedVisitAt <= toUtc &&
                request.VisitRequestStatusId != VisitRequestStatusIds.Rejected &&
                request.VisitRequestStatusId != VisitRequestStatusIds.Cancelled)
            .Select(request => new CalendarEventDto
            {
                Id = request.Id,
                EventType = CalendarEventTypes.Visit,
                Title = $"Posjet — {request.Resident!.FirstName} {request.Resident.LastName}",
                Subtitle = null,
                StatusName = request.VisitRequestStatus!.Name,
                StartsAt = request.RequestedVisitAt,
                EndsAt = null
            })
            .ToListAsync();
    }

    private async Task<List<CalendarEventDto>> GetActivityEventsAsync(
        DateTime fromUtc,
        DateTime toUtc)
    {
        return await _context.Activities
            .AsNoTracking()
            .Include(activity => activity.ActivityType)
            .Where(activity =>
                activity.StartsAt <= toUtc &&
                (activity.EndsAt == null || activity.EndsAt >= fromUtc))
            .Select(activity => new CalendarEventDto
            {
                Id = activity.Id,
                EventType = CalendarEventTypes.Activity,
                Title = activity.Title,
                Subtitle = activity.Location,
                StatusName = activity.ActivityType!.Name,
                StartsAt = activity.StartsAt,
                EndsAt = activity.EndsAt
            })
            .ToListAsync();
    }

    private static int? GetFamilyContactId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(AppClaimTypes.FamilyContactId)?.Value
                    ?? user.FindFirstValue(AppClaimTypes.FamilyContactId);

        return int.TryParse(claim, out var familyContactId) ? familyContactId : null;
    }
}
