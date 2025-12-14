using SnapRoll.Domain.Enums;

namespace SnapRoll.Application.DTOs;

/// <summary>
/// Request to create a new session.
/// </summary>
public class CreateSessionRequest
{
    /// <summary>
    /// Course ID to create the session for.
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Optional late threshold in seconds (default: 60).
    /// </summary>
    public int? LateThresholdSeconds { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? MaxDistanceMeters { get; set; }
}

/// <summary>
/// Response after creating a session.
/// </summary>
public class SessionResponse
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string SessionCode { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsActive { get; set; }
    public int LateThresholdSeconds { get; set; }
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
}

/// <summary>
/// Request to scan a QR code.
/// </summary>
public class ScanRequest
{
    /// <summary>
    /// The session ID to mark attendance for.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// The QR token from the scanned code.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Device metadata (User-Agent, OS, etc.).
    /// </summary>
    public string? DeviceMetadata { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

/// <summary>
/// Response after a scan attempt.
/// </summary>
public class ScanResponse
{
    public bool Success { get; set; }
    public ScanResult Result { get; set; }
    public string Message { get; set; } = string.Empty;
    public AttendanceStatus? AttendanceStatus { get; set; }
    public DateTime? ScannedAt { get; set; }
}

/// <summary>
/// Real-time statistics for a session.
/// </summary>
public class SessionStatsResponse
{
    public Guid SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime StartTime { get; set; }
    public int TotalEnrolled { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int PendingCount { get; set; }
    public double AttendancePercentage { get; set; }
    public List<AttendanceRecordDto> RecentScans { get; set; } = new();
}

/// <summary>
/// Individual attendance record for display.
/// </summary>
public class AttendanceRecordDto
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string UniversityId { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; }
    public DateTime? ScannedAt { get; set; }
}
