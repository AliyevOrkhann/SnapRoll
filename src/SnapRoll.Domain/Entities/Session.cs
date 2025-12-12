namespace SnapRoll.Domain.Entities;

/// <summary>
/// Represents an attendance session for a course.
/// </summary>
public class Session
{
    /// <summary>
    /// Unique identifier for the session.
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
    /// Unique code for this session (human-readable identifier).
    /// </summary>
    public string SessionCode { get; set; } = string.Empty;

    /// <summary>
    /// When the session started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the session ended (null if still active).
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Indicates if the session is currently accepting attendance.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Late threshold in seconds after StartTime.
    /// Default is 60 seconds (1 minute).
    /// </summary>
    public int LateThresholdSeconds { get; set; } = 60;

    /// <summary>
    /// Attendance records for this session.
    /// </summary>
    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    /// <summary>
    /// Scan logs for this session.
    /// </summary>
    public virtual ICollection<ScanLog> ScanLogs { get; set; } = new List<ScanLog>();

    /// <summary>
    /// Calculates the late threshold time.
    /// </summary>
    public DateTime LateThresholdTime => StartTime.AddSeconds(LateThresholdSeconds);
}
