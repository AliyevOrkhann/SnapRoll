using Microsoft.AspNetCore.Identity;
using SnapRoll.Domain.Enums;

namespace SnapRoll.Domain.Entities;

/// <summary>
/// Represents a user in the SnapRoll system.
/// Extends IdentityUser with university-specific properties.
/// </summary>
public class AppUser : IdentityUser
{
    /// <summary>
    /// Full name of the user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// University-assigned unique identifier (student ID or employee ID).
    /// </summary>
    public string UniversityId { get; set; } = string.Empty;

    /// <summary>
    /// Type of user (Student, Instructor, or Admin).
    /// </summary>
    public UserType UserType { get; set; }

    /// <summary>
    /// Courses taught by this user (only applicable for Instructors).
    /// </summary>
    public virtual ICollection<Course> TaughtCourses { get; set; } = new List<Course>();

    /// <summary>
    /// Courses this user is enrolled in (only applicable for Students).
    /// </summary>
    public virtual ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();

    /// <summary>
    /// Attendance records for this user (only applicable for Students).
    /// </summary>
    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    /// <summary>
    /// Scan logs for this user (only applicable for Students).
    /// </summary>
    public virtual ICollection<ScanLog> ScanLogs { get; set; } = new List<ScanLog>();
}
