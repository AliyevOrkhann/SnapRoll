namespace SnapRoll.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between students and courses.
/// </summary>
public class CourseEnrollment
{
    /// <summary>
    /// Unique identifier for the enrollment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the course.
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Navigation property to the course.
    /// </summary>
    public virtual Course Course { get; set; } = null!;

    /// <summary>
    /// Foreign key to the student.
    /// </summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the student.
    /// </summary>
    public virtual AppUser Student { get; set; } = null!;

    /// <summary>
    /// Date when the student enrolled in the course.
    /// </summary>
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the enrollment is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
