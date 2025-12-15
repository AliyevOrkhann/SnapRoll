using SnapRoll.Application.DTOs;

namespace SnapRoll.Application.Interfaces;

/// <summary>
/// Interface for attendance recording operations.
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Processes a QR code scan and records attendance.
    /// Validates token, checks for duplicates, and logs the attempt.
    /// </summary>
    Task<ScanResponse> ProcessScanAsync(string studentId, ScanRequest request, string? ipAddress);

    /// <summary>
    /// Gets attendance records for a session.
    /// </summary>
    Task<List<AttendanceRecordDto>> GetSessionAttendanceAsync(Guid sessionId);

    /// <summary>
    /// Gets real-time statistics for a session.
    /// </summary>
    Task<SessionStatsResponse> GetSessionStatsAsync(Guid sessionId);

    /// <summary>
    /// Marks all students without attendance as absent when session closes.
    /// </summary>
    Task FinalizeSessionAttendanceAsync(Guid sessionId);

    /// <summary>
    /// Checks if a student is enrolled in the course for a session.
    /// </summary>
    Task<bool> IsStudentEnrolledAsync(Guid sessionId, string studentId);

    /// <summary>
    /// Checks if a student has already scanned for a session.
    /// </summary>
    Task<bool> HasStudentScannedAsync(Guid sessionId, string studentId);

    /// <summary>
    /// Marks a student as present for a session (used by instructors/admins).
    /// </summary>
    Task MarkStudentPresentAsync(Guid sessionId, string studentId, string markedBy);

    /// <summary>
    /// Unmarks a student's attendance for a session (used by instructors/admins).
    /// Removes the attendance record so the student becomes pending again.
    /// </summary>
    Task UnmarkStudentAsync(Guid sessionId, string studentId, string markedBy);

    /// <summary>
    /// Gets attendance history for a student in a specific course.
    /// </summary>
    Task<List<StudentAttendanceHistoryDto>> GetStudentAttendanceHistoryAsync(string studentId, Guid courseId);
}
