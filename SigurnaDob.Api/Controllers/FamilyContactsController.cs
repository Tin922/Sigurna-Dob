using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Api.Security;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiController]
[Route("api/[controller]")]
public class FamilyContactsController : ControllerBase
{
    private const string DefaultFamilyPassword = "Family123!";
    private readonly SigurnaDobDbContext _context;

    public FamilyContactsController(SigurnaDobDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<FamilyContactDto>>> GetFamilyContacts(
        [FromQuery] int? residentId,
        [FromQuery] string? search,
        [FromQuery] string sortBy = "lastName",
        [FromQuery] string sortDir = "asc")
    {
        var query = _context.FamilyContacts
            .AsNoTracking()
            .Include(contact => contact.Resident)
            .AsQueryable();

        if (residentId.HasValue)
            query = query.Where(contact => contact.ResidentId == residentId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(contact =>
                contact.FirstName.ToLower().Contains(term) ||
                contact.LastName.ToLower().Contains(term) ||
                (contact.Relationship != null &&
                 contact.Relationship.ToLower().Contains(term)));
        }

        query = ApplySorting(query, sortBy, sortDir);

        var contacts = await query
            .Select(contact => new FamilyContactDto
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Relationship = contact.Relationship,
                Phone = contact.Phone,
                Email = contact.Email,
                ResidentId = contact.ResidentId,
                ResidentName = contact.Resident != null
                    ? contact.Resident.FirstName + " " + contact.Resident.LastName
                    : string.Empty,
                HasLinkedAccount = contact.AppUser != null
            })
            .ToListAsync();

        return Ok(contacts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FamilyContactDetailDto>> GetFamilyContactById(int id)
    {
        var contact = await _context.FamilyContacts
            .AsNoTracking()
            .Include(item => item.Resident)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (contact is null)
            return NotFound();

        return Ok(await BuildDetailDto(contact));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<FamilyContactDetailDto>> CreateFamilyContact(
        SaveFamilyContactDto dto)
    {
        var validationError = await ValidateSaveRequest(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        var contact = new FamilyContact
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Relationship = NormalizeOptional(dto.Relationship),
            Phone = NormalizeOptional(dto.Phone),
            Email = NormalizeOptional(dto.Email),
            Note = NormalizeOptional(dto.Note),
            ResidentId = dto.ResidentId
        };

        _context.FamilyContacts.Add(contact);
        await _context.SaveChangesAsync();

        if (dto.CreatePortalAccount)
        {
            var portalError = await TryCreatePortalAccountAsync(contact);
            if (portalError is not null)
                return BadRequest(portalError);
        }

        return CreatedAtAction(
            nameof(GetFamilyContactById),
            new { id = contact.Id },
            await LoadDetailDto(contact.Id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<FamilyContactDetailDto>> UpdateFamilyContact(
        int id,
        SaveFamilyContactDto dto)
    {
        var contact = await _context.FamilyContacts.FirstOrDefaultAsync(item => item.Id == id);
        if (contact is null)
            return NotFound();

        var validationError = await ValidateSaveRequest(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        contact.FirstName = dto.FirstName.Trim();
        contact.LastName = dto.LastName.Trim();
        contact.Relationship = NormalizeOptional(dto.Relationship);
        contact.Phone = NormalizeOptional(dto.Phone);
        contact.Email = NormalizeOptional(dto.Email);
        contact.Note = NormalizeOptional(dto.Note);
        contact.ResidentId = dto.ResidentId;

        await _context.SaveChangesAsync();

        if (dto.CreatePortalAccount)
        {
            var portalError = await TryCreatePortalAccountAsync(contact);
            if (portalError is not null)
                return BadRequest(portalError);
        }

        return Ok(await LoadDetailDto(id));
    }

    [Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
    [HttpPost("{id:int}/portal-account")]
    public async Task<ActionResult<FamilyContactDetailDto>> CreatePortalAccount(int id)
    {
        var contact = await _context.FamilyContacts.FirstOrDefaultAsync(item => item.Id == id);
        if (contact is null)
            return NotFound();

        var portalError = await TryCreatePortalAccountAsync(contact);
        if (portalError is not null)
            return BadRequest(portalError);

        return Ok(await LoadDetailDto(id));
    }

    private async Task<string?> TryCreatePortalAccountAsync(FamilyContact contact)
    {
        if (string.IsNullOrWhiteSpace(contact.Email))
            return "Email je obavezan za kreiranje portal računa.";

        if (await _context.AppUsers.AnyAsync(user => user.FamilyContactId == contact.Id))
            return "Kontakt već ima povezan portal račun.";

        var email = contact.Email.Trim().ToLowerInvariant();
        if (await _context.AppUsers.AnyAsync(user => user.Email == email))
            return "Email je već registriran na drugom računu.";

        var roleIds = await _context.AppRoles
            .Where(role => role.Name == AppRoles.User || role.Name == AppRoles.FamilyMember)
            .Select(role => role.Id)
            .ToListAsync();

        if (roleIds.Count != 2)
            return "Uloge za portal obitelji nisu ispravno postavljene.";

        var hasher = new PasswordHasher<AppUser>();
        var user = new AppUser
        {
            Email = email,
            DisplayName = $"{contact.FirstName} {contact.LastName}".Trim(),
            FamilyContactId = contact.Id
        };
        user.PasswordHash = hasher.HashPassword(user, DefaultFamilyPassword);

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        foreach (var roleId in roleIds)
        {
            _context.AppUserRoles.Add(new AppUserRole
            {
                AppUserId = user.Id,
                AppRoleId = roleId
            });
        }

        await _context.SaveChangesAsync();
        return null;
    }

    private async Task<string?> ValidateSaveRequest(SaveFamilyContactDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return "Ime i prezime kontakta su obavezni.";

        var residentExists = await _context.Residents
            .AnyAsync(resident => resident.Id == dto.ResidentId);
        if (!residentExists)
            return "Odabrani korisnik doma ne postoji.";

        return null;
    }

    private async Task<FamilyContactDetailDto> LoadDetailDto(int id)
    {
        var contact = await _context.FamilyContacts
            .AsNoTracking()
            .Include(item => item.Resident)
            .FirstAsync(item => item.Id == id);

        return await BuildDetailDto(contact);
    }

    private async Task<FamilyContactDetailDto> BuildDetailDto(FamilyContact contact)
    {
        var hasLinkedAccount = await _context.AppUsers
            .AnyAsync(user => user.FamilyContactId == contact.Id);

        var visitRequestCount = await _context.VisitRequests
            .CountAsync(request => request.FamilyContactId == contact.Id);

        return new FamilyContactDetailDto
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Relationship = contact.Relationship,
            Phone = contact.Phone,
            Email = contact.Email,
            Note = contact.Note,
            ResidentId = contact.ResidentId,
            ResidentName = contact.Resident != null
                ? $"{contact.Resident.FirstName} {contact.Resident.LastName}"
                : string.Empty,
            HasLinkedAccount = hasLinkedAccount,
            VisitRequestCount = visitRequestCount
        };
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<FamilyContact> ApplySorting(
        IQueryable<FamilyContact> query,
        string sortBy,
        string sortDir)
    {
        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("firstname", false) => query.OrderBy(contact => contact.FirstName),
            ("firstname", true) => query.OrderByDescending(contact => contact.FirstName),
            ("relationship", false) => query.OrderBy(contact => contact.Relationship),
            ("relationship", true) => query.OrderByDescending(contact => contact.Relationship),
            ("resident", false) => query.OrderBy(contact => contact.Resident!.LastName),
            ("resident", true) => query.OrderByDescending(contact => contact.Resident!.LastName),
            ("lastname", true) => query.OrderByDescending(contact => contact.LastName),
            _ => query.OrderBy(contact => contact.LastName)
        };
    }
}
