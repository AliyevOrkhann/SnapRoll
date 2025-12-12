namespace SnapRoll.Domain.Enums;

/// <summary>
/// Defines the attendance status for a student in a session.
/// </summary>
public enum AttendanceStatus
{
    /// <summary>
    /// Student has not been marked (default state before session ends).
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Student scanned within the on-time window.
    /// </summary>
    Present = 1,

    /// <summary>
    /// Student scanned after the late threshold (>1 minute after session start).
    /// </summary>
    Late = 2,

    /// <summary>
    /// Student did not scan during the session.
    /// </summary>
    Absent = 3
}
