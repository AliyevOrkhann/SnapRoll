namespace SnapRoll.Domain.Entities;

/// <summary>
/// Represents a course in the university system.
/// </summary>
public class Course
{
    /// <summary>
    /// Unique identifier for the course.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Course code (e.g., CSCI3509).
    /// </summary>
    public string CourseCode { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the course.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the course.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Foreign key to the instructor teaching this course.
    /// </summary>
    public string InstructorId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the instructor.
    /// </summary>
    public virtual AppUser Instructor { get; set; } = null!;

    /// <summary>
    /// Sessions held for this course.
    /// </summary>
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    /// <summary>
    /// Student enrollments in this course.
    /// </summary>
    public virtual ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();

    /// <summary>
    /// Timestamp when the course was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the course is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
