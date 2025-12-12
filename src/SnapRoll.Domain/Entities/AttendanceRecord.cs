using SnapRoll.Domain.Enums;

namespace SnapRoll.Domain.Entities;

/// <summary>
/// Represents a student's attendance record for a specific session.
/// </summary>
public class AttendanceRecord
{
    /// <summary>
    /// Unique identifier for the attendance record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the session.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Navigation property to the session.
    /// </summary>
    public virtual Session Session { get; set; } = null!;

    /// <summary>
    /// Foreign key to the student.
    /// </summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the student.
    /// </summary>
    public virtual AppUser Student { get; set; } = null!;

    /// <summary>
    /// Timestamp when the student scanned the QR code.
    /// Null if student was marked absent.
    /// </summary>
    public DateTime? ScannedAt { get; set; }

    /// <summary>
    /// The attendance status (Present, Late, Absent).
    /// </summary>
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Pending;

    /// <summary>
    /// Timestamp when the record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
