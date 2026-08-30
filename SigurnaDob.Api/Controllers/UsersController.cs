using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Api.Security;
using SigurnaDob.Api.Services;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly SigurnaDobDbContext _context;

    public UsersController(SigurnaDobDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AppUserDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int? roleId,
        [FromQuery] string sortBy = "displayName",
        [FromQuery] string sortDir = "asc")
    {
        var query = BuildQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(user =>
                user.Email.ToLower().Contains(term) ||
                user.DisplayName.ToLower().Contains(term));
        }

        if (isActive.HasValue)
            query = query.Where(user => user.IsActive == isActive.Value);

        if (roleId.HasValue)
            query = query.Where(user => user.UserRoles.Any(link => link.AppRoleId == roleId.Value));

        query = ApplySorting(query, sortBy, sortDir);

        var users = await query.ToListAsync();
        return Ok(users.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppUserDetailDto>> GetUserById(int id)
    {
        var user = await BuildQuery().FirstOrDefaultAsync(item => item.Id == id);
        if (user is null)
            return NotFound();

        return Ok(ToDetailDto(user));
    }

    [HttpPost]
    public async Task<ActionResult<AppUserDetailDto>> CreateUser(SaveAppUserDto dto)
    {
        var validationError = AppUserBusinessRules.ValidateCreate(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _context.AppUsers.AnyAsync(user => user.Email == email))
            return BadRequest("Email je već registriran.");

        validationError = await ValidateRolesAndProfileAsync(dto, null);
        if (validationError is not null)
            return BadRequest(validationError);

        var hasher = new PasswordHasher<AppUser>();
        var user = new AppUser
        {
            Email = email,
            DisplayName = dto.DisplayName.Trim(),
            IsActive = dto.IsActive,
            EmployeeId = dto.EmployeeId,
            FamilyContactId = dto.FamilyContactId,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, dto.Password!);

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        await SyncRolesAsync(user.Id, dto.RoleIds);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, await LoadDetailDto(user.Id));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppUserDetailDto>> UpdateUser(int id, SaveAppUserDto dto)
    {
        var user = await _context.AppUsers
            .Include(item => item.UserRoles)
            .ThenInclude(link => link.AppRole)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (user is null)
            return NotFound();

        var validationError = AppUserBusinessRules.ValidateUpdate(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _context.AppUsers.AnyAsync(item => item.Id != id && item.Email == email))
            return BadRequest("Email je već registriran.");

        validationError = await ValidateRolesAndProfileAsync(dto, id);
        if (validationError is not null)
            return BadRequest(validationError);

        var currentUserId = GetCurrentUserId();
        if (currentUserId == id)
        {
            if (!dto.IsActive)
                return BadRequest("Ne možete deaktivirati vlastiti račun.");

            var hadAdmin = user.UserRoles.Any(link => link.AppRole?.Name == AppRoles.Admin);
            var roles = await ResolveRoleNamesAsync(dto.RoleIds);
            if (hadAdmin && !roles.Contains(AppRoles.Admin))
                return BadRequest("Ne možete ukloniti administratorsku ulogu s vlastitog računa.");
        }

        if (!dto.IsActive && user.UserRoles.Any(link => link.AppRole?.Name == AppRoles.Admin))
        {
            validationError = await ValidateLastActiveAdminAsync(id);
            if (validationError is not null)
                return BadRequest(validationError);
        }

        var newRoleNames = await ResolveRoleNamesAsync(dto.RoleIds);
        if (!newRoleNames.Contains(AppRoles.Admin) &&
            user.UserRoles.Any(link => link.AppRole?.Name == AppRoles.Admin))
        {
            validationError = await ValidateLastActiveAdminAsync(id);
            if (validationError is not null)
                return BadRequest(validationError);
        }

        user.Email = email;
        user.DisplayName = dto.DisplayName.Trim();
        user.IsActive = dto.IsActive;
        user.EmployeeId = dto.EmployeeId;
        user.FamilyContactId = dto.FamilyContactId;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var hasher = new PasswordHasher<AppUser>();
            user.PasswordHash = hasher.HashPassword(user, dto.Password);
        }

        await SyncRolesAsync(user.Id, dto.RoleIds);
        await _context.SaveChangesAsync();

        return Ok(await LoadDetailDto(id));
    }

    [HttpPut("{id:int}/deactivate")]
    public async Task<ActionResult<AppUserDetailDto>> DeactivateUser(int id)
    {
        var user = await _context.AppUsers
            .Include(item => item.UserRoles)
            .ThenInclude(link => link.AppRole)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (user is null)
            return NotFound();

        if (GetCurrentUserId() == id)
            return BadRequest("Ne možete deaktivirati vlastiti račun.");

        if (!user.IsActive)
            return Ok(await LoadDetailDto(id));

        if (user.UserRoles.Any(link => link.AppRole?.Name == AppRoles.Admin))
        {
            var validationError = await ValidateLastActiveAdminAsync(id);
            if (validationError is not null)
                return BadRequest(validationError);
        }

        user.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(await LoadDetailDto(id));
    }

    [HttpPut("{id:int}/activate")]
    public async Task<ActionResult<AppUserDetailDto>> ActivateUser(int id)
    {
        var user = await _context.AppUsers.FirstOrDefaultAsync(item => item.Id == id);
        if (user is null)
            return NotFound();

        user.IsActive = true;
        await _context.SaveChangesAsync();

        return Ok(await LoadDetailDto(id));
    }

    private IQueryable<AppUser> BuildQuery() =>
        _context.AppUsers
            .AsNoTracking()
            .Include(user => user.UserRoles)
            .ThenInclude(link => link.AppRole)
            .Include(user => user.Employee)
            .Include(user => user.FamilyContact);

    private async Task<string?> ValidateRolesAndProfileAsync(SaveAppUserDto dto, int? userId)
    {
        var distinctRoleIds = dto.RoleIds.Distinct().ToList();
        var roles = await _context.AppRoles
            .Where(role => distinctRoleIds.Contains(role.Id))
            .ToListAsync();

        if (roles.Count != distinctRoleIds.Count)
            return "Odabrana je nepostojeća uloga.";

        var roleNames = roles.Select(role => role.Name).ToList();
        var profileError = AppUserBusinessRules.ValidateProfileLink(
            roleNames, dto.EmployeeId, dto.FamilyContactId);
        if (profileError is not null)
            return profileError;

        if (dto.EmployeeId.HasValue)
        {
            if (!await _context.Employees.AnyAsync(employee => employee.Id == dto.EmployeeId.Value))
                return "Odabrani djelatnik ne postoji.";

            if (await _context.AppUsers.AnyAsync(user =>
                    user.EmployeeId == dto.EmployeeId.Value &&
                    (!userId.HasValue || user.Id != userId.Value)))
                return "Djelatnik je već povezan s drugim računom.";
        }

        if (dto.FamilyContactId.HasValue)
        {
            if (!await _context.FamilyContacts.AnyAsync(contact => contact.Id == dto.FamilyContactId.Value))
                return "Odabrani obiteljski kontakt ne postoji.";

            if (await _context.AppUsers.AnyAsync(user =>
                    user.FamilyContactId == dto.FamilyContactId.Value &&
                    (!userId.HasValue || user.Id != userId.Value)))
                return "Obiteljski kontakt je već povezan s drugim računom.";
        }

        return null;
    }

    private async Task<string?> ValidateLastActiveAdminAsync(int userId)
    {
        var adminRoleId = await _context.AppRoles
            .Where(role => role.Name == AppRoles.Admin)
            .Select(role => role.Id)
            .FirstAsync();

        var activeAdminCount = await _context.AppUsers
            .Where(user => user.IsActive)
            .CountAsync(user => user.UserRoles.Any(link => link.AppRoleId == adminRoleId));

        if (activeAdminCount <= 1)
            return "Sustav mora imati barem jednog aktivnog administratora.";

        return null;
    }

    private async Task<List<string>> ResolveRoleNamesAsync(IEnumerable<int> roleIds) =>
        await _context.AppRoles
            .Where(role => roleIds.Contains(role.Id))
            .Select(role => role.Name)
            .ToListAsync();

    private async Task SyncRolesAsync(int userId, IEnumerable<int> roleIds)
    {
        var existing = await _context.AppUserRoles
            .Where(link => link.AppUserId == userId)
            .ToListAsync();

        _context.AppUserRoles.RemoveRange(existing);

        foreach (var roleId in roleIds.Distinct())
        {
            _context.AppUserRoles.Add(new AppUserRole
            {
                AppUserId = userId,
                AppRoleId = roleId
            });
        }
    }

    private async Task<AppUserDetailDto> LoadDetailDto(int id)
    {
        var user = await BuildQuery().FirstAsync(item => item.Id == id);
        return ToDetailDto(user);
    }

    private static AppUserDto ToDto(AppUser user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles
                .Where(link => link.AppRole is not null)
                .Select(link => link.AppRole!.DisplayName)
                .OrderBy(name => name)
                .ToList(),
            ProfileSummary = BuildProfileSummary(user)
        };

    private static AppUserDetailDto ToDetailDto(AppUser user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            RoleIds = user.UserRoles.Select(link => link.AppRoleId).ToList(),
            RoleNames = user.UserRoles
                .Where(link => link.AppRole is not null)
                .Select(link => link.AppRole!.DisplayName)
                .OrderBy(name => name)
                .ToList(),
            EmployeeId = user.EmployeeId,
            EmployeeName = user.Employee != null
                ? $"{user.Employee.FirstName} {user.Employee.LastName}"
                : null,
            FamilyContactId = user.FamilyContactId,
            FamilyContactName = user.FamilyContact != null
                ? $"{user.FamilyContact.FirstName} {user.FamilyContact.LastName}"
                : null
        };

    private static string? BuildProfileSummary(AppUser user)
    {
        if (user.Employee is not null)
            return $"Djelatnik: {user.Employee.FirstName} {user.Employee.LastName}";

        if (user.FamilyContact is not null)
            return $"Obitelj: {user.FamilyContact.FirstName} {user.FamilyContact.LastName}";

        return null;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    private static IQueryable<AppUser> ApplySorting(
        IQueryable<AppUser> query,
        string sortBy,
        string sortDir)
    {
        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("email", false) => query.OrderBy(user => user.Email),
            ("email", true) => query.OrderByDescending(user => user.Email),
            ("createdat", false) => query.OrderBy(user => user.CreatedAt),
            ("createdat", true) => query.OrderByDescending(user => user.CreatedAt),
            ("active", false) => query.OrderBy(user => user.IsActive),
            ("active", true) => query.OrderByDescending(user => user.IsActive),
            ("displayname", true) => query.OrderByDescending(user => user.DisplayName),
            _ => query.OrderBy(user => user.DisplayName)
        };
    }
}
