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

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VisitRequestsController : ControllerBase
{
    private readonly SigurnaDobDbContext _context;

    public VisitRequestsController(SigurnaDobDbContext context)
    {
        _context = context;
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<VisitRequestDto>>> GetVisitRequests(
        [FromQuery] string? search,
        [FromQuery] int? statusId,
        [FromQuery] int? residentId,
        [FromQuery] int? familyContactId,
        [FromQuery] DateTime? visitBefore,
        [FromQuery] DateTime? visitAfter,
        [FromQuery] string sortBy = "requestedVisitAt",
        [FromQuery] string sortDir = "asc")
    {
        var query = BuildQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(request =>
                (request.Resident != null &&
                 (request.Resident.FirstName + " " + request.Resident.LastName).ToLower().Contains(term)) ||
                (request.FamilyContact != null &&
                 (request.FamilyContact.FirstName + " " + request.FamilyContact.LastName).ToLower().Contains(term)));
        }

        if (statusId.HasValue)
            query = query.Where(request => request.VisitRequestStatusId == statusId.Value);

        if (residentId.HasValue)
            query = query.Where(request => request.ResidentId == residentId.Value);

        if (familyContactId.HasValue)
            query = query.Where(request => request.FamilyContactId == familyContactId.Value);

        if (visitBefore.HasValue)
            query = query.Where(request => request.RequestedVisitAt <= visitBefore.Value);

        if (visitAfter.HasValue)
            query = query.Where(request => request.RequestedVisitAt >= visitAfter.Value);

        query = ApplySorting(query, sortBy, sortDir);

        var requests = await query.ToListAsync();
        return Ok(requests.Select(ToDto).ToList());
    }

    [Authorize(Roles = AppRoles.FamilyMember)]
    [HttpGet("mine")]
    public async Task<ActionResult<List<VisitRequestDto>>> GetMyVisitRequests()
    {
        var familyContactId = GetCurrentFamilyContactId();
        if (familyContactId is null)
            return Forbid();

        var requests = await BuildQuery()
            .Where(request => request.FamilyContactId == familyContactId.Value)
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync();

        return Ok(requests.Select(ToDto).ToList());
    }

    [Authorize(Roles = AppRoles.FamilyMember)]
    [HttpPost("mine")]
    public async Task<ActionResult<VisitRequestDetailDto>> CreateMyVisitRequest(
        SaveVisitRequestDto dto)
    {
        var familyContactId = GetCurrentFamilyContactId();
        if (familyContactId is null)
            return Forbid();

        var contact = await _context.FamilyContacts
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == familyContactId.Value);

        if (contact is null)
            return BadRequest("Obiteljski kontakt nije pronađen.");

        var validationError = VisitRequestBusinessRules.ValidateNewRequest(
            dto.RequestedVisitAt, DateTime.UtcNow);
        if (validationError is not null)
            return BadRequest(validationError);

        var now = DateTime.UtcNow;
        var request = new VisitRequest
        {
            ResidentId = contact.ResidentId,
            FamilyContactId = contact.Id,
            VisitRequestStatusId = VisitRequestStatusIds.Received,
            RequestedAt = now,
            RequestedVisitAt = dto.RequestedVisitAt,
            UpdatedAt = now
        };

        _context.VisitRequests.Add(request);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetVisitRequestById),
            new { id = request.Id },
            await LoadDetailDto(request.Id));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VisitRequestDetailDto>> GetVisitRequestById(int id)
    {
        var request = await BuildQuery().FirstOrDefaultAsync(item => item.Id == id);
        if (request is null)
            return NotFound();

        if (!CanAccessRequest(request))
            return Forbid();

        return Ok(ToDetailDto(request));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPut("{id:int}/approve")]
    public async Task<ActionResult<VisitRequestDetailDto>> ApproveVisitRequest(
        int id,
        DecideVisitRequestDto dto)
    {
        var request = await _context.VisitRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (request is null)
            return NotFound();

        var validationError = VisitRequestBusinessRules.ValidateApprove(request, DateTime.UtcNow);
        if (validationError is not null)
            return BadRequest(validationError);

        var coordinatorId = await ResolveCoordinatorIdAsync();
        if (coordinatorId is null)
            return BadRequest("Nije moguće odrediti koordinatora.");

        request.VisitRequestStatusId = VisitRequestStatusIds.Approved;
        request.CoordinatorId = coordinatorId;
        request.DecidedAt = DateTime.UtcNow;
        request.DecisionNote = NormalizeOptional(dto.DecisionNote);
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(await LoadDetailDto(id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPut("{id:int}/reject")]
    public async Task<ActionResult<VisitRequestDetailDto>> RejectVisitRequest(
        int id,
        DecideVisitRequestDto dto)
    {
        var request = await _context.VisitRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (request is null)
            return NotFound();

        var validationError = VisitRequestBusinessRules.ValidateReject(request);
        if (validationError is not null)
            return BadRequest(validationError);

        var coordinatorId = await ResolveCoordinatorIdAsync();
        if (coordinatorId is null)
            return BadRequest("Nije moguće odrediti koordinatora.");

        request.VisitRequestStatusId = VisitRequestStatusIds.Rejected;
        request.CoordinatorId = coordinatorId;
        request.DecidedAt = DateTime.UtcNow;
        request.DecisionNote = NormalizeOptional(dto.DecisionNote);
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(await LoadDetailDto(id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPut("{id:int}/held")]
    public async Task<ActionResult<VisitRequestDetailDto>> MarkVisitHeld(int id)
    {
        var request = await _context.VisitRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (request is null)
            return NotFound();

        var validationError = VisitRequestBusinessRules.ValidateMarkHeld(request);
        if (validationError is not null)
            return BadRequest(validationError);

        request.VisitRequestStatusId = VisitRequestStatusIds.Held;
        request.HeldAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(await LoadDetailDto(id));
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<VisitRequestDetailDto>> CancelVisitRequest(int id)
    {
        var request = await _context.VisitRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (request is null)
            return NotFound();

        var isFamily = User.IsInRole(AppRoles.FamilyMember) &&
                       !User.IsInRole(AppRoles.Admin) &&
                       !User.IsInRole(AppRoles.Coordinator);

        if (isFamily)
        {
            var familyContactId = GetCurrentFamilyContactId();
            if (familyContactId is null || request.FamilyContactId != familyContactId.Value)
                return Forbid();
        }
        else if (!User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.Coordinator))
        {
            return Forbid();
        }

        var validationError = VisitRequestBusinessRules.ValidateCancel(request, isFamily);
        if (validationError is not null)
            return BadRequest(validationError);

        request.VisitRequestStatusId = VisitRequestStatusIds.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(await LoadDetailDto(id));
    }

    private IQueryable<VisitRequest> BuildQuery() =>
        _context.VisitRequests
            .AsNoTracking()
            .Include(request => request.Resident)
            .Include(request => request.FamilyContact)
            .Include(request => request.VisitRequestStatus)
            .Include(request => request.Coordinator);

    private bool CanAccessRequest(VisitRequest request)
    {
        if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Coordinator))
            return true;

        if (User.IsInRole(AppRoles.FamilyMember))
        {
            var familyContactId = GetCurrentFamilyContactId();
            return familyContactId.HasValue && request.FamilyContactId == familyContactId.Value;
        }

        return false;
    }

    private int? GetCurrentFamilyContactId()
    {
        var claim = User.FindFirst(AppClaimTypes.FamilyContactId)?.Value
                    ?? User.FindFirstValue(AppClaimTypes.FamilyContactId);

        return int.TryParse(claim, out var familyContactId) ? familyContactId : null;
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

    private async Task<VisitRequestDetailDto> LoadDetailDto(int id)
    {
        var request = await BuildQuery().FirstAsync(item => item.Id == id);
        return ToDetailDto(request);
    }

    private static VisitRequestDto ToDto(VisitRequest request) =>
        new()
        {
            Id = request.Id,
            ResidentId = request.ResidentId,
            ResidentName = request.Resident != null
                ? $"{request.Resident.FirstName} {request.Resident.LastName}"
                : string.Empty,
            FamilyContactId = request.FamilyContactId,
            FamilyContactName = request.FamilyContact != null
                ? $"{request.FamilyContact.FirstName} {request.FamilyContact.LastName}"
                : string.Empty,
            VisitRequestStatusId = request.VisitRequestStatusId,
            StatusName = request.VisitRequestStatus?.Name ?? string.Empty,
            RequestedAt = request.RequestedAt,
            RequestedVisitAt = request.RequestedVisitAt,
            DecidedAt = request.DecidedAt,
            HeldAt = request.HeldAt,
            DecisionNote = request.DecisionNote
        };

    private static VisitRequestDetailDto ToDetailDto(VisitRequest request) =>
        new()
        {
            Id = request.Id,
            ResidentId = request.ResidentId,
            ResidentName = request.Resident != null
                ? $"{request.Resident.FirstName} {request.Resident.LastName}"
                : string.Empty,
            FamilyContactId = request.FamilyContactId,
            FamilyContactName = request.FamilyContact != null
                ? $"{request.FamilyContact.FirstName} {request.FamilyContact.LastName}"
                : string.Empty,
            VisitRequestStatusId = request.VisitRequestStatusId,
            StatusName = request.VisitRequestStatus?.Name ?? string.Empty,
            CoordinatorId = request.CoordinatorId,
            CoordinatorName = request.Coordinator != null
                ? $"{request.Coordinator.FirstName} {request.Coordinator.LastName}"
                : null,
            RequestedAt = request.RequestedAt,
            RequestedVisitAt = request.RequestedVisitAt,
            DecidedAt = request.DecidedAt,
            HeldAt = request.HeldAt,
            DecisionNote = request.DecisionNote,
            UpdatedAt = request.UpdatedAt
        };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<VisitRequest> ApplySorting(
        IQueryable<VisitRequest> query,
        string sortBy,
        string sortDir)
    {
        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("requestedat", false) => query.OrderBy(request => request.RequestedAt),
            ("requestedat", true) => query.OrderByDescending(request => request.RequestedAt),
            ("status", false) => query.OrderBy(request => request.VisitRequestStatus!.Name),
            ("status", true) => query.OrderByDescending(request => request.VisitRequestStatus!.Name),
            ("resident", false) => query.OrderBy(request => request.Resident!.LastName),
            ("resident", true) => query.OrderByDescending(request => request.Resident!.LastName),
            ("requestedvisitat", true) => query.OrderByDescending(request => request.RequestedVisitAt),
            _ => query.OrderBy(request => request.RequestedVisitAt)
        };
    }
}
