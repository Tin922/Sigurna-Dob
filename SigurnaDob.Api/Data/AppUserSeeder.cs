using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Data;

public static class AppUserSeeder
{
    public static async Task SeedAsync(SigurnaDobDbContext db)
    {
        if (await db.AppUsers.AnyAsync())
            return;

        var coordinatorEmployeeId = await db.Employees
            .Where(employee => employee.EmployeeCode == "EMP-001")
            .Select(employee => employee.Id)
            .FirstAsync();

        var caregiverEmployeeId = await db.Employees
            .Where(employee => employee.EmployeeCode == "EMP-002")
            .Select(employee => employee.Id)
            .FirstAsync();

        var familyContactId = await db.FamilyContacts
            .Select(contact => contact.Id)
            .FirstAsync();

        var hasher = new PasswordHasher<AppUser>();

        var admin = new AppUser
        {
            Email = "admin@sigurna-dob.local",
            DisplayName = "Sigurna dob Admin"
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

        var coordinator = new AppUser
        {
            Email = "coordinator@sigurna-dob.local",
            DisplayName = "Demo koordinator",
            EmployeeId = coordinatorEmployeeId
        };
        coordinator.PasswordHash = hasher.HashPassword(coordinator, "Coordinator123!");

        var caregiver = new AppUser
        {
            Email = "caregiver@sigurna-dob.local",
            DisplayName = "Demo njegovatelj",
            EmployeeId = caregiverEmployeeId
        };
        caregiver.PasswordHash = hasher.HashPassword(caregiver, "Caregiver123!");

        var familyMember = new AppUser
        {
            Email = "family@sigurna-dob.local",
            DisplayName = "Demo član obitelji",
            FamilyContactId = familyContactId
        };
        familyMember.PasswordHash = hasher.HashPassword(familyMember, "Family123!");

        db.AppUsers.AddRange(admin, coordinator, caregiver, familyMember);
        await db.SaveChangesAsync();

        var roleIds = await db.AppRoles.ToDictionaryAsync(role => role.Name, role => role.Id);

        db.AppUserRoles.AddRange(
            new AppUserRole { AppUserId = admin.Id, AppRoleId = roleIds[AppRoles.User] },
            new AppUserRole { AppUserId = admin.Id, AppRoleId = roleIds[AppRoles.Admin] },
            new AppUserRole { AppUserId = coordinator.Id, AppRoleId = roleIds[AppRoles.User] },
            new AppUserRole { AppUserId = coordinator.Id, AppRoleId = roleIds[AppRoles.Coordinator] },
            new AppUserRole { AppUserId = caregiver.Id, AppRoleId = roleIds[AppRoles.User] },
            new AppUserRole { AppUserId = caregiver.Id, AppRoleId = roleIds[AppRoles.Caregiver] },
            new AppUserRole { AppUserId = familyMember.Id, AppRoleId = roleIds[AppRoles.User] },
            new AppUserRole { AppUserId = familyMember.Id, AppRoleId = roleIds[AppRoles.FamilyMember] });

        await db.SaveChangesAsync();
    }
}
