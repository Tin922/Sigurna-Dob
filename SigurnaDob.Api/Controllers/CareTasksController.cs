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
public class CareTasksController : ControllerBase
{
    private readonly SigurnaDobDbContext _context;
    private readonly ChangeHistoryService _changeHistory;
    private readonly CaregiverWorkloadService _caregiverWorkloadService;

    public CareTasksController(
        SigurnaDobDbContext context,
        ChangeHistoryService changeHistory,
        CaregiverWorkloadService caregiverWorkloadService)
    {
        _context = context;
        _changeHistory = changeHistory;
        _caregiverWorkloadService = caregiverWorkloadService;
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<CareTaskDto>>> GetCareTasks(
        [FromQuery] string? search,
        [FromQuery] int? statusId,
        [FromQuery] int? typeId,
        [FromQuery] int? caregiverId,
        [FromQuery] int? residentId,
        [FromQuery] DateTime? dueBefore,
        [FromQuery] DateTime? dueAfter,
        [FromQuery] string sortBy = "dueAt",
        [FromQuery] string sortDir = "asc")
    {
        var query = BuildQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(task =>
                task.Title.ToLower().Contains(term) ||
                (task.Description != null && task.Description.ToLower().Contains(term)) ||
                (task.Resident != null &&
                 (task.Resident.FirstName + " " + task.Resident.LastName).ToLower().Contains(term)));
        }

        if (statusId.HasValue)
            query = query.Where(task => task.CareTaskStatusId == statusId.Value);

        if (typeId.HasValue)
            query = query.Where(task => task.CareTaskTypeId == typeId.Value);

        if (caregiverId.HasValue)
            query = query.Where(task => task.CaregiverId == caregiverId.Value);

        if (residentId.HasValue)
            query = query.Where(task => task.ResidentId == residentId.Value);

        if (dueBefore.HasValue)
            query = query.Where(task => task.DueAt != null && task.DueAt <= dueBefore.Value);

        if (dueAfter.HasValue)
            query = query.Where(task => task.DueAt != null && task.DueAt >= dueAfter.Value);

        query = ApplySorting(query, sortBy, sortDir);

        var tasks = await query.ToListAsync();
        return Ok(tasks.Select(task => ToDto(task)).ToList());
    }

    [HttpGet("workload")]
    public async Task<ActionResult<CaregiverWorkloadOverviewDto>> GetCaregiverWorkload()
    {
        if (!User.IsInRole(AppRoles.Admin) &&
            !User.IsInRole(AppRoles.Coordinator) &&
            !User.IsInRole(AppRoles.Caregiver))
            return Forbid();

        return Ok(await _caregiverWorkloadService.GetOverviewAsync(User));
    }

    [Authorize(Roles = AppRoles.Caregiver)]
    [HttpGet("mine")]
    public async Task<ActionResult<List<CareTaskDto>>> GetMyCareTasks()
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Forbid();

        var tasks = await BuildQuery()
            .Where(task => task.CaregiverId == employeeId.Value)
            .OrderBy(task => task.DueAt ?? DateTime.MaxValue)
            .ThenBy(task => task.Title)
            .ToListAsync();

        return Ok(tasks.Select(task => ToDto(task)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CareTaskDetailDto>> GetCareTaskById(int id)
    {
        var task = await BuildQuery().FirstOrDefaultAsync(item => item.Id == id);
        if (task is null)
            return NotFound();

        if (!CanAccessTask(task))
            return Forbid();

        return Ok(ToDetailDto(task));
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<List<CareTaskChangeHistoryDto>>> GetCareTaskHistory(int id)
    {
        var task = await _context.CareTasks.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (task is null)
            return NotFound();

        if (!CanAccessTask(task))
            return Forbid();

        return Ok(await _changeHistory.GetCareTaskHistoryAsync(id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<CareTaskDetailDto>> CreateCareTask(SaveCareTaskDto dto)
    {
        var validationError = await ValidateSaveRequestAsync(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        var coordinatorId = await ResolveCoordinatorIdAsync();
        if (coordinatorId is null)
            return BadRequest("Nije moguće odrediti koordinatora zadatka.");

        var statusId = dto.CareTaskStatusId == CareTaskStatusIds.Cancelled
            ? CareTaskStatusIds.Cancelled
            : CareTaskBusinessRules.ResolveInitialStatus(dto.CaregiverId);

        var now = DateTime.UtcNow;
        var task = new CareTask
        {
            Title = dto.Title.Trim(),
            Description = NormalizeOptional(dto.Description),
            ResidentId = dto.ResidentId,
            CareTaskTypeId = dto.CareTaskTypeId,
            CareTaskStatusId = statusId,
            CoordinatorId = coordinatorId.Value,
            CaregiverId = dto.CaregiverId,
            DueAt = dto.DueAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.CareTasks.Add(task);
        await _context.SaveChangesAsync();

        _changeHistory.RecordCareTaskStatusChange(task.Id, null, statusId, User);
        if (dto.CaregiverId.HasValue)
        {
            _changeHistory.RecordCareTaskAssignmentChange(
                task.Id,
                null,
                dto.CaregiverId,
                User);
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCareTaskById),
            new { id = task.Id },
            await LoadDetailDto(task.Id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CareTaskDetailDto>> UpdateCareTask(int id, SaveCareTaskDto dto)
    {
        var task = await _context.CareTasks.FirstOrDefaultAsync(item => item.Id == id);
        if (task is null)
            return NotFound();

        var validationError = await ValidateSaveRequestAsync(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        var updateError = CareTaskBusinessRules.ValidateCoordinatorUpdate(task, dto);
        if (updateError is not null)
            return BadRequest(updateError);

        var previousStatusId = task.CareTaskStatusId;
        var previousCaregiverId = task.CaregiverId;

        task.Title = dto.Title.Trim();
        task.Description = NormalizeOptional(dto.Description);
        task.ResidentId = dto.ResidentId;
        task.CareTaskTypeId = dto.CareTaskTypeId;
        task.CaregiverId = dto.CaregiverId;
        task.DueAt = dto.DueAt;
        task.UpdatedAt = DateTime.UtcNow;

        if (dto.CareTaskStatusId == CareTaskStatusIds.Cancelled)
        {
            task.CareTaskStatusId = CareTaskStatusIds.Cancelled;
        }
        else if (task.CareTaskStatusId != CareTaskStatusIds.InProgress)
        {
            task.CareTaskStatusId =
                CareTaskBusinessRules.ResolveStatusOnAssignment(dto.CaregiverId, task.CareTaskStatusId);
        }

        _changeHistory.RecordCareTaskAssignmentChange(
            task.Id,
            previousCaregiverId,
            task.CaregiverId,
            User);
        _changeHistory.RecordCareTaskStatusChange(
            task.Id,
            previousStatusId,
            task.CareTaskStatusId,
            User);

        await _context.SaveChangesAsync();
        return Ok(await LoadDetailDto(id));
    }

    [Authorize(Roles = AppRoles.Caregiver)]
    [HttpPut("{id:int}/start")]
    public async Task<ActionResult<CareTaskDetailDto>> StartCareTask(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Forbid();

        var task = await _context.CareTasks.FirstOrDefaultAsync(item => item.Id == id);
        if (task is null)
            return NotFound();

        var startError = CareTaskBusinessRules.ValidateStart(task, employeeId.Value);
        if (startError is not null)
            return BadRequest(startError);

        var previousStatusId = task.CareTaskStatusId;
        task.CareTaskStatusId = CareTaskStatusIds.InProgress;
        task.StartedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        _changeHistory.RecordCareTaskStatusChange(
            task.Id,
            previousStatusId,
            task.CareTaskStatusId,
            User);

        await _context.SaveChangesAsync();
        return Ok(await LoadDetailDto(id));
    }

    [Authorize(Roles = AppRoles.Caregiver)]
    [HttpPut("{id:int}/complete")]
    public async Task<ActionResult<CareTaskDetailDto>> CompleteCareTask(
        int id,
        CompleteCareTaskDto dto)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId is null)
            return Forbid();

        var task = await _context.CareTasks.FirstOrDefaultAsync(item => item.Id == id);
        if (task is null)
            return NotFound();

        var completeError = CareTaskBusinessRules.ValidateComplete(task, employeeId.Value, dto);
        if (completeError is not null)
            return BadRequest(completeError);

        var previousStatusId = task.CareTaskStatusId;
        task.CareTaskStatusId = CareTaskStatusIds.Completed;
        task.CompletedAt = dto.CompletedAt ?? DateTime.UtcNow;
        task.CompletionNote = dto.CompletionNote.Trim();
        task.UpdatedAt = DateTime.UtcNow;

        _changeHistory.RecordCareTaskStatusChange(
            task.Id,
            previousStatusId,
            task.CareTaskStatusId,
            User);

        await _context.SaveChangesAsync();
        return Ok(await LoadDetailDto(id));
    }

    private IQueryable<CareTask> BuildQuery() =>
        _context.CareTasks
            .AsNoTracking()
            .Include(task => task.Resident)
            .Include(task => task.CareTaskType)
            .Include(task => task.CareTaskStatus)
            .Include(task => task.Caregiver)
            .Include(task => task.Coordinator);

    private bool CanAccessTask(CareTask task)
    {
        if (User.IsInRole(AppRoles.Admin) ||
            User.IsInRole(AppRoles.Coordinator))
            return true;

        if (!User.IsInRole(AppRoles.Caregiver))
            return false;

        var employeeId = GetCurrentEmployeeId();
        return employeeId.HasValue && task.CaregiverId == employeeId.Value;
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

    private async Task<string?> ValidateSaveRequestAsync(SaveCareTaskDto dto)
    {
        var basicError = CareTaskBusinessRules.ValidateSave(dto);
        if (basicError is not null)
            return basicError;

        if (!await _context.Residents.AnyAsync(resident => resident.Id == dto.ResidentId))
            return "Odabrani korisnik doma ne postoji.";

        if (!await _context.CareTaskTypes.AnyAsync(type => type.Id == dto.CareTaskTypeId))
            return "Odabrana vrsta zadatka ne postoji.";

        if (!await _context.CareTaskStatuses.AnyAsync(status => status.Id == dto.CareTaskStatusId))
            return "Odabrani status zadatka ne postoji.";

        if (dto.CaregiverId.HasValue &&
            !await _context.Employees.AnyAsync(employee => employee.Id == dto.CaregiverId.Value))
            return "Odabrani njegovatelj ne postoji.";

        return null;
    }

    private async Task<CareTaskDetailDto> LoadDetailDto(int id)
    {
        var task = await BuildQuery().FirstAsync(item => item.Id == id);
        return ToDetailDto(task);
    }

    private CareTaskDto ToDto(CareTask task)
    {
        var utcNow = DateTime.UtcNow;
        return new CareTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            ResidentId = task.ResidentId,
            ResidentName = task.Resident != null
                ? $"{task.Resident.FirstName} {task.Resident.LastName}"
                : string.Empty,
            CareTaskTypeId = task.CareTaskTypeId,
            TypeName = task.CareTaskType?.Name ?? string.Empty,
            CareTaskStatusId = task.CareTaskStatusId,
            StatusName = task.CareTaskStatus?.Name ?? string.Empty,
            CaregiverId = task.CaregiverId,
            CaregiverName = task.Caregiver != null
                ? $"{task.Caregiver.FirstName} {task.Caregiver.LastName}"
                : null,
            DueAt = task.DueAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            IsOverdue = CareTaskBusinessRules.IsOverdue(task, utcNow)
        };
    }

    private static CareTaskDetailDto ToDetailDto(CareTask task)
    {
        var utcNow = DateTime.UtcNow;
        return new CareTaskDetailDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            ResidentId = task.ResidentId,
            ResidentName = task.Resident != null
                ? $"{task.Resident.FirstName} {task.Resident.LastName}"
                : string.Empty,
            CareTaskTypeId = task.CareTaskTypeId,
            TypeName = task.CareTaskType?.Name ?? string.Empty,
            CareTaskStatusId = task.CareTaskStatusId,
            StatusName = task.CareTaskStatus?.Name ?? string.Empty,
            CoordinatorId = task.CoordinatorId,
            CoordinatorName = task.Coordinator != null
                ? $"{task.Coordinator.FirstName} {task.Coordinator.LastName}"
                : string.Empty,
            CaregiverId = task.CaregiverId,
            CaregiverName = task.Caregiver != null
                ? $"{task.Caregiver.FirstName} {task.Caregiver.LastName}"
                : null,
            DueAt = task.DueAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            CompletionNote = task.CompletionNote,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            IsOverdue = CareTaskBusinessRules.IsOverdue(task, utcNow)
        };
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<CareTask> ApplySorting(
        IQueryable<CareTask> query,
        string sortBy,
        string sortDir)
    {
        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("title", false) => query.OrderBy(task => task.Title),
            ("title", true) => query.OrderByDescending(task => task.Title),
            ("status", false) => query.OrderBy(task => task.CareTaskStatus!.Name),
            ("status", true) => query.OrderByDescending(task => task.CareTaskStatus!.Name),
            ("type", false) => query.OrderBy(task => task.CareTaskType!.Name),
            ("type", true) => query.OrderByDescending(task => task.CareTaskType!.Name),
            ("resident", false) => query.OrderBy(task => task.Resident!.LastName),
            ("resident", true) => query.OrderByDescending(task => task.Resident!.LastName),
            ("dueat", true) => query.OrderByDescending(task => task.DueAt),
            _ => query.OrderBy(task => task.DueAt)
        };
    }
}
