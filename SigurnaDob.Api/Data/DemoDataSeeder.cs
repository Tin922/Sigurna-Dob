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

        if (!await db.CareTasks.AnyAsync())
        {
            var residentId = await db.Residents.Select(resident => resident.Id).FirstAsync();
            var coordinatorId = await db.Employees
                .Where(employee => employee.EmployeeCode == "EMP-001")
                .Select(employee => employee.Id)
                .FirstAsync();
            var caregiverId = await db.Employees
                .Where(employee => employee.EmployeeCode == "EMP-002")
                .Select(employee => employee.Id)
                .FirstAsync();

            var now = DateTime.UtcNow;

            db.CareTasks.AddRange(
                new CareTask
                {
                    Title = "Jutarnja terapija",
                    Description = "Davanje jutarnih lijekova.",
                    ResidentId = residentId,
                    CareTaskTypeId = 1,
                    CareTaskStatusId = 1,
                    CoordinatorId = coordinatorId,
                    DueAt = now.AddHours(8)
                },
                new CareTask
                {
                    Title = "Pomoć pri obroku",
                    Description = "Ručak u blagovaonici.",
                    ResidentId = residentId,
                    CareTaskTypeId = 2,
                    CareTaskStatusId = 2,
                    CoordinatorId = coordinatorId,
                    CaregiverId = caregiverId,
                    DueAt = now.AddHours(4)
                },
                new CareTask
                {
                    Title = "Higijena i odjeća",
                    Description = "Priprema odjeće za popodnevni odlazak.",
                    ResidentId = residentId,
                    CareTaskTypeId = 3,
                    CareTaskStatusId = 3,
                    CoordinatorId = coordinatorId,
                    CaregiverId = caregiverId,
                    StartedAt = now.AddHours(-1),
                    DueAt = now.AddHours(2)
                },
                new CareTask
                {
                    Title = "Popodnevna šetnja",
                    Description = "Kratka šetnja u dvorištu.",
                    ResidentId = residentId,
                    CareTaskTypeId = 4,
                    CareTaskStatusId = 4,
                    CoordinatorId = coordinatorId,
                    CaregiverId = caregiverId,
                    StartedAt = now.AddDays(-1),
                    CompletedAt = now.AddDays(-1).AddHours(1),
                    CompletionNote = "Šetnja odrađena bez poteškoća.",
                    DueAt = now.AddDays(-1)
                },
                new CareTask
                {
                    Title = "Administrativni upit",
                    Description = "Kontakt s obitelji oko posjeta.",
                    ResidentId = residentId,
                    CareTaskTypeId = 5,
                    CareTaskStatusId = 2,
                    CoordinatorId = coordinatorId,
                    CaregiverId = caregiverId,
                    DueAt = now.AddDays(-1)
                });
        }

        await db.SaveChangesAsync();
    }
}
