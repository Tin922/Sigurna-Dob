using Microsoft.EntityFrameworkCore;
using SigurnaDob.Shared.Models;

namespace SigurnaDob.Api.Data;

public class SigurnaDobDbContext : DbContext
{
    public SigurnaDobDbContext(DbContextOptions<SigurnaDobDbContext> options)
        : base(options)
    {
    }

    public DbSet<ResidentStatus> ResidentStatuses => Set<ResidentStatus>();
    public DbSet<RoomStatus> RoomStatuses => Set<RoomStatus>();
    public DbSet<EmployeePosition> EmployeePositions => Set<EmployeePosition>();
    public DbSet<EmployeeStatus> EmployeeStatuses => Set<EmployeeStatus>();
    public DbSet<CareTaskType> CareTaskTypes => Set<CareTaskType>();
    public DbSet<CareTaskStatus> CareTaskStatuses => Set<CareTaskStatus>();
    public DbSet<VisitRequestStatus> VisitRequestStatuses => Set<VisitRequestStatus>();
    public DbSet<ActivityType> ActivityTypes => Set<ActivityType>();

    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<FamilyContact> FamilyContacts => Set<FamilyContact>();
    public DbSet<CareTask> CareTasks => Set<CareTask>();
    public DbSet<VisitRequest> VisitRequests => Set<VisitRequest>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ResidentActivity> ResidentActivities => Set<ResidentActivity>();
    public DbSet<ResidentMedia> ResidentMedia => Set<ResidentMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ResidentStatus>(entity =>
        {
            entity.Property(status => status.Name).HasMaxLength(100);
            entity.HasIndex(status => status.Name).IsUnique();
        });

        modelBuilder.Entity<RoomStatus>(entity =>
        {
            entity.Property(status => status.Name).HasMaxLength(100);
            entity.HasIndex(status => status.Name).IsUnique();
        });

        modelBuilder.Entity<EmployeePosition>(entity =>
        {
            entity.Property(position => position.Name).HasMaxLength(100);
            entity.HasIndex(position => position.Name).IsUnique();
        });

        modelBuilder.Entity<EmployeeStatus>(entity =>
        {
            entity.Property(status => status.Name).HasMaxLength(100);
            entity.HasIndex(status => status.Name).IsUnique();
        });

        modelBuilder.Entity<CareTaskType>(entity =>
        {
            entity.Property(type => type.Name).HasMaxLength(100);
            entity.HasIndex(type => type.Name).IsUnique();
        });

        modelBuilder.Entity<CareTaskStatus>(entity =>
        {
            entity.Property(status => status.Name).HasMaxLength(100);
            entity.HasIndex(status => status.Name).IsUnique();
        });

        modelBuilder.Entity<VisitRequestStatus>(entity =>
        {
            entity.Property(status => status.Name).HasMaxLength(100);
            entity.HasIndex(status => status.Name).IsUnique();
        });

        modelBuilder.Entity<ActivityType>(entity =>
        {
            entity.Property(type => type.Name).HasMaxLength(100);
            entity.HasIndex(type => type.Name).IsUnique();
        });

        modelBuilder.Entity<AppRole>()
            .HasIndex(role => role.Name)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.EmployeeId)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.FamilyContactId)
            .IsUnique();

        modelBuilder.Entity<AppUserRole>()
            .HasKey(userRole => new { userRole.AppUserId, userRole.AppRoleId });

        modelBuilder.Entity<AppUserRole>()
            .HasOne(userRole => userRole.AppUser)
            .WithMany(user => user.UserRoles)
            .HasForeignKey(userRole => userRole.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppUserRole>()
            .HasOne(userRole => userRole.AppRole)
            .WithMany(role => role.UserRoles)
            .HasForeignKey(userRole => userRole.AppRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasOne(user => user.Employee)
            .WithOne(employee => employee.AppUser)
            .HasForeignKey<AppUser>(user => user.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AppUser>()
            .HasOne(user => user.FamilyContact)
            .WithOne(contact => contact.AppUser)
            .HasForeignKey<AppUser>(user => user.FamilyContactId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Employee>()
            .HasIndex(employee => employee.EmployeeCode)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasOne(employee => employee.EmployeePosition)
            .WithMany(position => position.Employees)
            .HasForeignKey(employee => employee.EmployeePositionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(employee => employee.EmployeeStatus)
            .WithMany(status => status.Employees)
            .HasForeignKey(employee => employee.EmployeeStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Room>()
            .HasIndex(room => room.Name)
            .IsUnique();

        modelBuilder.Entity<Room>()
            .HasOne(room => room.RoomStatus)
            .WithMany(status => status.Rooms)
            .HasForeignKey(room => room.RoomStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Resident>()
            .HasOne(resident => resident.ResidentStatus)
            .WithMany(status => status.Residents)
            .HasForeignKey(resident => resident.ResidentStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Resident>()
            .HasOne(resident => resident.Room)
            .WithMany(room => room.Residents)
            .HasForeignKey(resident => resident.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FamilyContact>()
            .HasOne(contact => contact.Resident)
            .WithMany(resident => resident.FamilyContacts)
            .HasForeignKey(contact => contact.ResidentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CareTask>()
            .HasOne(task => task.Resident)
            .WithMany(resident => resident.CareTasks)
            .HasForeignKey(task => task.ResidentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CareTask>()
            .HasOne(task => task.CareTaskType)
            .WithMany(type => type.CareTasks)
            .HasForeignKey(task => task.CareTaskTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CareTask>()
            .HasOne(task => task.CareTaskStatus)
            .WithMany(status => status.CareTasks)
            .HasForeignKey(task => task.CareTaskStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CareTask>()
            .HasOne(task => task.Coordinator)
            .WithMany(employee => employee.CoordinatedCareTasks)
            .HasForeignKey(task => task.CoordinatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CareTask>()
            .HasOne(task => task.Caregiver)
            .WithMany(employee => employee.AssignedCareTasks)
            .HasForeignKey(task => task.CaregiverId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VisitRequest>()
            .HasOne(request => request.Resident)
            .WithMany(resident => resident.VisitRequests)
            .HasForeignKey(request => request.ResidentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VisitRequest>()
            .HasOne(request => request.FamilyContact)
            .WithMany(contact => contact.VisitRequests)
            .HasForeignKey(request => request.FamilyContactId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VisitRequest>()
            .HasOne(request => request.Coordinator)
            .WithMany(employee => employee.CoordinatedVisitRequests)
            .HasForeignKey(request => request.CoordinatorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VisitRequest>()
            .HasOne(request => request.VisitRequestStatus)
            .WithMany(status => status.VisitRequests)
            .HasForeignKey(request => request.VisitRequestStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Activity>()
            .HasOne(activity => activity.ActivityType)
            .WithMany(type => type.Activities)
            .HasForeignKey(activity => activity.ActivityTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Activity>()
            .HasOne(activity => activity.Coordinator)
            .WithMany(employee => employee.CoordinatedActivities)
            .HasForeignKey(activity => activity.CoordinatorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ResidentActivity>()
            .HasKey(link => new { link.ResidentId, link.ActivityId });

        modelBuilder.Entity<ResidentActivity>()
            .HasOne(link => link.Resident)
            .WithMany(resident => resident.ResidentActivities)
            .HasForeignKey(link => link.ResidentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResidentActivity>()
            .HasOne(link => link.Activity)
            .WithMany(activity => activity.ResidentActivities)
            .HasForeignKey(link => link.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResidentMedia>()
            .Property(media => media.MediaType)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<ResidentMedia>()
            .HasOne(media => media.Resident)
            .WithMany(resident => resident.Media)
            .HasForeignKey(media => media.ResidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
