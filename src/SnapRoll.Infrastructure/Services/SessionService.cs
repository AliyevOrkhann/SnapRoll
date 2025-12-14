using System.Text;
using Microsoft.EntityFrameworkCore;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;
using SnapRoll.Domain.Entities;
using SnapRoll.Domain.Enums;
using SnapRoll.Infrastructure.Data;

namespace SnapRoll.Infrastructure.Services;

/// <summary>
/// Service for session lifecycle management.
/// </summary>
public class SessionService : ISessionService
{
    private readonly SnapRollDbContext _context;
    private readonly IAttendanceService _attendanceService;

    public SessionService(SnapRollDbContext context, IAttendanceService attendanceService)
    {
        _context = context;
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// Creates a new attendance session for a course.
    /// </summary>
    public async Task<SessionResponse> CreateSessionAsync(string instructorId, CreateSessionRequest request)
    {
        // Verify instructor owns the course
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId && c.InstructorId == instructorId);

        if (course == null)
            throw new UnauthorizedAccessException("You are not the instructor for this course");

        // Check for existing active session
        var existingActive = await _context.Sessions
            .AnyAsync(s => s.CourseId == request.CourseId && s.IsActive);

        if (existingActive)
            throw new InvalidOperationException("An active session already exists for this course");

        var now = DateTime.UtcNow;
        var sessionCode = GenerateSessionCode();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            CourseId = request.CourseId,
            SessionCode = sessionCode,
            StartTime = now,
            IsActive = true,
            LateThresholdSeconds = request.LateThresholdSeconds ?? 60,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            MaxDistanceMeters = request.MaxDistanceMeters ?? 50
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return await MapToResponseAsync(session, course);
    }

    /// <summary>
    /// Closes an active session.
    /// </summary>
    public async Task<SessionResponse> CloseSessionAsync(Guid sessionId, string instructorId)
    {
        var session = await _context.Sessions
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            throw new ArgumentException("Session not found", nameof(sessionId));

        if (session.Course.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You are not the instructor for this session");

        if (!session.IsActive)
            throw new InvalidOperationException("Session is already closed");

        session.IsActive = false;
        session.EndTime = DateTime.UtcNow;

        // Mark absent students
        await _attendanceService.FinalizeSessionAttendanceAsync(sessionId);

        await _context.SaveChangesAsync();

        return await MapToResponseAsync(session, session.Course);
    }

    /// <summary>
    /// Gets session details by ID.
    /// </summary>
    public async Task<SessionResponse?> GetSessionAsync(Guid sessionId)
    {
        var session = await _context.Sessions
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return null;

        return await MapToResponseAsync(session, session.Course);
    }

    /// <summary>
    /// Gets all sessions for a course.
    /// </summary>
    public async Task<List<SessionResponse>> GetCourseSessionsAsync(Guid courseId)
    {
        var sessions = await _context.Sessions
            .Include(s => s.Course)
            .Where(s => s.CourseId == courseId)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();

        var responses = new List<SessionResponse>();
        foreach (var session in sessions)
        {
            responses.Add(await MapToResponseAsync(session, session.Course));
        }
        return responses;
    }

    /// <summary>
    /// Gets all sessions for a course ensuring the caller is the course instructor.
    /// </summary>
    public async Task<List<SessionResponse>> GetCourseSessionsForInstructorAsync(Guid courseId, string instructorId)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null)
            throw new ArgumentException("Course not found", nameof(courseId));

        if (course.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You are not the instructor for this course");

        var sessions = await _context.Sessions
            .Include(s => s.Course)
            .Where(s => s.CourseId == courseId)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();

        var responses = new List<SessionResponse>();
        foreach (var session in sessions)
        {
            responses.Add(await MapToResponseAsync(session, session.Course));
        }

        return responses;
    }

    /// <summary>
    /// Gets all active sessions for an instructor.
    /// </summary>
    public async Task<List<SessionResponse>> GetActiveSessionsAsync(string instructorId)
    {
        var sessions = await _context.Sessions
            .Include(s => s.Course)
            .Where(s => s.IsActive && s.Course.InstructorId == instructorId)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();

        var responses = new List<SessionResponse>();
        foreach (var session in sessions)
        {
            responses.Add(await MapToResponseAsync(session, session.Course));
        }
        return responses;
    }

    /// <summary>
    /// Verifies if the user is the instructor for a session.
    /// </summary>
    public async Task<bool> IsSessionInstructorAsync(Guid sessionId, string userId)
    {
        return await _context.Sessions
            .Include(s => s.Course)
            .AnyAsync(s => s.Id == sessionId && s.Course.InstructorId == userId);
    }

    /// <summary>
    /// Exports attendance data as CSV.
    /// </summary>
    public async Task<byte[]> ExportAttendanceCsvAsync(Guid sessionId, string instructorId)
    {
        var session = await _context.Sessions
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            throw new ArgumentException("Session not found", nameof(sessionId));

        if (session.Course.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You are not the instructor for this session");

        var records = await _context.AttendanceRecords
            .Where(a => a.SessionId == sessionId)
            .Include(a => a.Student)
            .OrderBy(a => a.Student.FullName)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("UniversityId,FullName,Email,Status,ScannedAt");

        foreach (var record in records)
        {
            // Stored timestamps are UTC. Convert to local time for CSV readability.
            var scannedAt = record.ScannedAt.HasValue
                ? record.ScannedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : "";

            sb.AppendLine($"{record.Student.UniversityId},{record.Student.FullName},{record.Student.Email},{record.Status},{scannedAt}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<SessionResponse> MapToResponseAsync(Session session, Course course)
    {
        var totalStudents = await _context.CourseEnrollments
            .CountAsync(e => e.CourseId == session.CourseId && e.IsActive);

        var attendanceCounts = await _context.AttendanceRecords
            .Where(a => a.SessionId == session.Id)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new SessionResponse
        {
            Id = session.Id,
            CourseId = session.CourseId,
            CourseCode = course.CourseCode,
            CourseName = course.Name,
            SessionCode = session.SessionCode,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            IsActive = session.IsActive,
            LateThresholdSeconds = session.LateThresholdSeconds,
            TotalStudents = totalStudents,
            PresentCount = attendanceCounts.FirstOrDefault(x => x.Status == AttendanceStatus.Present)?.Count ?? 0,
            LateCount = attendanceCounts.FirstOrDefault(x => x.Status == AttendanceStatus.Late)?.Count ?? 0,
            AbsentCount = attendanceCounts.FirstOrDefault(x => x.Status == AttendanceStatus.Absent)?.Count ?? 0
        };
    }

    private static string GenerateSessionCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
