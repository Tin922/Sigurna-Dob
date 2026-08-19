using Microsoft.EntityFrameworkCore;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(SigurnaDobDbContext db)
    {
        if (!await db.Employees.AnyAsync())
        {
            db.Employees.AddRange(
                new Employee
                {
                    FirstName = "Petra",
                    LastName = "Novak",
                    EmployeeCode = "EMP-001",
                    Email = "petra.novak@sigurna-dob.example.com",
                    Phone = "031 100 200",
                    HiredAt = new DateOnly(2023, 3, 1),
                    Note = "Koordinator skrbi u domu.",
                    EmployeePositionId = 1,
                    EmployeeStatusId = 1
                },
                new Employee
                {
                    FirstName = "Ivan",
                    LastName = "Marić",
                    EmployeeCode = "EMP-002",
                    Email = "ivan.maric@sigurna-dob.example.com",
                    Phone = "031 100 201",
                    HiredAt = new DateOnly(2022, 9, 10),
                    Note = "Njegovatelj na jutarnjoj smjeni.",
                    EmployeePositionId = 2,
                    EmployeeStatusId = 1
                });
        }

        if (!await db.Rooms.AnyAsync())
        {
            db.Rooms.Add(new Room
            {
                Name = "Soba 101",
                Capacity = 2,
                Note = "Demo soba na prizemlju.",
                RoomStatusId = 1
            });
        }

        await db.SaveChangesAsync();

        if (!await db.Residents.AnyAsync())
        {
            var roomId = await db.Rooms.Select(room => room.Id).FirstAsync();

            db.Residents.Add(new Resident
            {
                FirstName = "Mara",
                LastName = "Horvat",
                DateOfBirth = new DateOnly(1945, 6, 12),
                PersonalId = "45061234567",
                ResidentStatusId = 2,
                RoomId = roomId,
                AdmittedAt = new DateOnly(2024, 1, 15),
                Note = "Demo korisnica doma za testiranje."
            });
        }

        await db.SaveChangesAsync();

        if (!await db.FamilyContacts.AnyAsync())
        {
            var residentId = await db.Residents.Select(resident => resident.Id).FirstAsync();

            db.FamilyContacts.Add(new FamilyContact
            {
                ResidentId = residentId,
                FirstName = "Ana",
                LastName = "Horvat",
                Relationship = "Kći",
                Phone = "091 222 3333",
                Email = "ana.horvat@example.com",
                Note = "Primarni kontakt obitelji."
            });
        }

        await db.SaveChangesAsync();
    }
}
