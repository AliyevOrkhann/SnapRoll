using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SnapRoll.Domain.Entities;

namespace SnapRoll.Infrastructure.Data;

/// <summary>
/// Database context for SnapRoll application.
/// Integrates ASP.NET Core Identity with custom entities.
/// </summary>
public class SnapRollDbContext : IdentityDbContext<AppUser>
{
    public SnapRollDbContext(DbContextOptions<SnapRollDbContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<ScanLog> ScanLogs => Set<ScanLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure AppUser
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.UniversityId)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(e => e.UniversityId)
                .IsUnique();

            entity.HasIndex(e => e.UserType);
        });

        // Configure Course
        builder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CourseCode)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.HasIndex(e => e.CourseCode)
                .IsUnique();

            entity.HasOne(e => e.Instructor)
                .WithMany(u => u.TaughtCourses)
                .HasForeignKey(e => e.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure CourseEnrollment (Many-to-Many join table)
        builder.Entity<CourseEnrollment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.CourseId, e.StudentId })
                .IsUnique();

            entity.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Session
        builder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SessionCode)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.SessionCode)
                .IsUnique();

            entity.HasIndex(e => e.IsActive);

            entity.HasIndex(e => new { e.CourseId, e.StartTime });

            entity.HasOne(e => e.Course)
                .WithMany(c => c.Sessions)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AttendanceRecord
        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique constraint: one record per student per session
            entity.HasIndex(e => new { e.SessionId, e.StudentId })
                .IsUnique();

            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.AttendanceRecords)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Student)
                .WithMany(u => u.AttendanceRecords)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ScanLog
        builder.Entity<ScanLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TokenUsed)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.DeviceMetadata)
                .HasMaxLength(1000);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45); // Max length for IPv6

            entity.Property(e => e.Notes)
                .HasMaxLength(500);

            entity.HasIndex(e => e.ScannedAt);

            entity.HasIndex(e => e.ScanResult);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.ScanLogs)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Student)
                .WithMany(u => u.ScanLogs)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
