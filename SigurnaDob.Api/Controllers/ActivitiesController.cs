using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Api.Security;
using SigurnaDob.Api.Services;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiController]
[Route("api/[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly SigurnaDobDbContext _context;

    public ActivitiesController(SigurnaDobDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ActivityDto>>> GetActivities(
        [FromQuery] string? search,
        [FromQuery] int? typeId,
        [FromQuery] DateTime? startsBefore,
        [FromQuery] DateTime? startsAfter,
        [FromQuery] string sortBy = "startsAt",
        [FromQuery] string sortDir = "asc")
    {
        var query = BuildQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(activity =>
                activity.Title.ToLower().Contains(term) ||
                (activity.Description != null && activity.Description.ToLower().Contains(term)) ||
                (activity.Location != null && activity.Location.ToLower().Contains(term)));
        }

        if (typeId.HasValue)
            query = query.Where(activity => activity.ActivityTypeId == typeId.Value);

        if (startsBefore.HasValue)
            query = query.Where(activity => activity.StartsAt <= startsBefore.Value);

        if (startsAfter.HasValue)
            query = query.Where(activity => activity.StartsAt >= startsAfter.Value);

        query = ApplySorting(query, sortBy, sortDir);

        var activities = await query.ToListAsync();
        return Ok(activities.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ActivityDetailDto>> GetActivityById(int id)
    {
        var activity = await BuildQuery(includeParticipants: true)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (activity is null)
            return NotFound();

        return Ok(ToDetailDto(activity));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<ActivityDetailDto>> CreateActivity(SaveActivityDto dto)
    {
        var validationError = await ValidateSaveRequestAsync(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        var coordinatorId = await ResolveCoordinatorIdAsync();
        if (coordinatorId is null)
            return BadRequest("Nije moguće odrediti koordinatora aktivnosti.");

        var now = DateTime.UtcNow;
        var activity = new Activity
        {
            Title = dto.Title.Trim(),
            Description = NormalizeOptional(dto.Description),
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt,
            Location = NormalizeOptional(dto.Location),
            ActivityTypeId = dto.ActivityTypeId,
            CoordinatorId = coordinatorId,
            UpdatedAt = now
        };

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetActivityById),
            new { id = activity.Id },
            await LoadDetailDto(activity.Id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ActivityDetailDto>> UpdateActivity(int id, SaveActivityDto dto)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(item => item.Id == id);
        if (activity is null)
            return NotFound();

        var validationError = await ValidateSaveRequestAsync(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        activity.Title = dto.Title.Trim();
        activity.Description = NormalizeOptional(dto.Description);
        activity.StartsAt = dto.StartsAt;
        activity.EndsAt = dto.EndsAt;
        activity.Location = NormalizeOptional(dto.Location);
        activity.ActivityTypeId = dto.ActivityTypeId;
        activity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(await LoadDetailDto(id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteActivity(int id)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(item => item.Id == id);
        if (activity is null)
            return NotFound();

        _context.Activities.Remove(activity);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPost("{id:int}/participants")]
    public async Task<ActionResult<ActivityDetailDto>> EnrollParticipant(
        int id,
        EnrollActivityParticipantDto dto)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(item => item.Id == id);
        if (activity is null)
            return NotFound();

        var resident = await _context.Residents.FirstOrDefaultAsync(item => item.Id == dto.ResidentId);
        var alreadyEnrolled = await _context.ResidentActivities.AnyAsync(link =>
            link.ActivityId == id && link.ResidentId == dto.ResidentId);

        var validationError = ActivityBusinessRules.ValidateEnrollment(resident, alreadyEnrolled);
        if (validationError is not null)
            return BadRequest(validationError);

        _context.ResidentActivities.Add(new ResidentActivity
        {
            ActivityId = id,
            ResidentId = dto.ResidentId,
            EnrolledAt = DateTime.UtcNow,
            Note = NormalizeOptional(dto.Note)
        });

        activity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(await LoadDetailDto(id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpDelete("{id:int}/participants/{residentId:int}")]
    public async Task<ActionResult<ActivityDetailDto>> RemoveParticipant(int id, int residentId)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(item => item.Id == id);
        if (activity is null)
            return NotFound();

        var link = await _context.ResidentActivities.FirstOrDefaultAsync(item =>
            item.ActivityId == id && item.ResidentId == residentId);

        if (link is null)
            return NotFound();

        _context.ResidentActivities.Remove(link);
        activity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(await LoadDetailDto(id));
    }

    private IQueryable<Activity> BuildQuery(bool includeParticipants = false)
    {
        IQueryable<Activity> query = _context.Activities
            .AsNoTracking()
            .Include(activity => activity.ActivityType)
            .Include(activity => activity.Coordinator);

        query = includeParticipants
            ? query
                .Include(activity => activity.ResidentActivities)
                .ThenInclude(link => link.Resident)
            : query.Include(activity => activity.ResidentActivities);

        return query;
    }

    private int? GetCurrentEmployeeId()
    {
        var claim = User.FindFirst(AppClaimTypes.EmployeeId)?.Value
                    ?? User.FindFirstValue(AppClaimTypes.EmployeeId);

        return int.TryParse(claim, out var employeeId) ? employeeId : null;
    }

    private async Task<int?> ResolveCoordinatorIdAsync()
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId.HasValue)
            return employeeId.Value;

        if (!User.IsInRole(AppRoles.Admin))
            return null;

        return await _context.Employees
            .Where(employee => employee.EmployeePositionId == 1 && employee.EmployeeStatusId == 1)
            .Select(employee => employee.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<string?> ValidateSaveRequestAsync(SaveActivityDto dto)
    {
        var basicError = ActivityBusinessRules.ValidateSave(dto);
        if (basicError is not null)
            return basicError;

        if (!await _context.ActivityTypes.AnyAsync(type => type.Id == dto.ActivityTypeId))
            return "Odabrana vrsta aktivnosti ne postoji.";

        return null;
    }

    private async Task<ActivityDetailDto> LoadDetailDto(int id)
    {
        var activity = await BuildQuery(includeParticipants: true).FirstAsync(item => item.Id == id);
        return ToDetailDto(activity);
    }

    private static ActivityDto ToDto(Activity activity) =>
        new()
        {
            Id = activity.Id,
            Title = activity.Title,
            Description = activity.Description,
            ActivityTypeId = activity.ActivityTypeId,
            TypeName = activity.ActivityType?.Name ?? string.Empty,
            StartsAt = activity.StartsAt,
            EndsAt = activity.EndsAt,
            Location = activity.Location,
            ParticipantCount = activity.ResidentActivities?.Count ?? 0,
            CoordinatorName = activity.Coordinator != null
                ? $"{activity.Coordinator.FirstName} {activity.Coordinator.LastName}"
                : null
        };

    private static ActivityDetailDto ToDetailDto(Activity activity) =>
        new()
        {
            Id = activity.Id,
            Title = activity.Title,
            Description = activity.Description,
            ActivityTypeId = activity.ActivityTypeId,
            TypeName = activity.ActivityType?.Name ?? string.Empty,
            CoordinatorId = activity.CoordinatorId,
            CoordinatorName = activity.Coordinator != null
                ? $"{activity.Coordinator.FirstName} {activity.Coordinator.LastName}"
                : null,
            StartsAt = activity.StartsAt,
            EndsAt = activity.EndsAt,
            Location = activity.Location,
            UpdatedAt = activity.UpdatedAt,
            Participants = activity.ResidentActivities
                .OrderBy(link => link.Resident!.LastName)
                .ThenBy(link => link.Resident!.FirstName)
                .Select(link => new ActivityParticipantDto
                {
                    ResidentId = link.ResidentId,
                    ResidentName = link.Resident != null
                        ? $"{link.Resident.FirstName} {link.Resident.LastName}"
                        : string.Empty,
                    EnrolledAt = link.EnrolledAt,
                    Note = link.Note
                })
                .ToList()
        };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<Activity> ApplySorting(
        IQueryable<Activity> query,
        string sortBy,
        string sortDir)
    {
        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("title", false) => query.OrderBy(activity => activity.Title),
            ("title", true) => query.OrderByDescending(activity => activity.Title),
            ("type", false) => query.OrderBy(activity => activity.ActivityType!.Name),
            ("type", true) => query.OrderByDescending(activity => activity.ActivityType!.Name),
            ("location", false) => query.OrderBy(activity => activity.Location),
            ("location", true) => query.OrderByDescending(activity => activity.Location),
            ("participants", false) => query.OrderBy(activity => activity.ResidentActivities.Count),
            ("participants", true) => query.OrderByDescending(activity => activity.ResidentActivities.Count),
            ("startsat", true) => query.OrderByDescending(activity => activity.StartsAt),
            _ => query.OrderBy(activity => activity.StartsAt)
        };
    }
}
