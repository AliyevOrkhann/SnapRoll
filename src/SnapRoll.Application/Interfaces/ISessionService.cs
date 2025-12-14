using SnapRoll.Application.DTOs;

namespace SnapRoll.Application.Interfaces;

/// <summary>
/// Interface for session management operations.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new attendance session for a course.
    /// </summary>
    Task<SessionResponse> CreateSessionAsync(string instructorId, CreateSessionRequest request);

    /// <summary>
    /// Closes an active session.
    /// </summary>
    Task<SessionResponse> CloseSessionAsync(Guid sessionId, string instructorId);

    /// <summary>
    /// Gets session details by ID.
    /// </summary>
    Task<SessionResponse?> GetSessionAsync(Guid sessionId);

    /// <summary>
    /// Gets all sessions for a course.
    /// </summary>
    Task<List<SessionResponse>> GetCourseSessionsAsync(Guid courseId);

    /// <summary>
    /// Gets all sessions for a course ensuring the caller is the course instructor.
    /// </summary>
    Task<List<SessionResponse>> GetCourseSessionsForInstructorAsync(Guid courseId, string instructorId);

    /// <summary>
    /// Gets all active sessions for an instructor.
    /// </summary>
    Task<List<SessionResponse>> GetActiveSessionsAsync(string instructorId);

    /// <summary>
    /// Verifies if the user is the instructor for a session.
    /// </summary>
    Task<bool> IsSessionInstructorAsync(Guid sessionId, string userId);

    /// <summary>
    /// Exports attendance data as CSV.
    /// </summary>
    Task<byte[]> ExportAttendanceCsvAsync(Guid sessionId, string instructorId);
}
