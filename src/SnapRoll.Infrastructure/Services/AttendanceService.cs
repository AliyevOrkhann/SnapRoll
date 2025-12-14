using Microsoft.EntityFrameworkCore;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;
using SnapRoll.Domain.Entities;
using SnapRoll.Domain.Enums;
using SnapRoll.Infrastructure.Data;

namespace SnapRoll.Infrastructure.Services;

/// <summary>
/// Service for processing attendance scans and managing attendance records.
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly SnapRollDbContext _context;
    private readonly IQrEngine _qrEngine;

    public AttendanceService(SnapRollDbContext context, IQrEngine qrEngine)
    {
        _context = context;
        _qrEngine = qrEngine;
    }

    /// <summary>
    /// Processes a QR code scan with full validation.
    /// </summary>
    public async Task<ScanResponse> ProcessScanAsync(string studentId, ScanRequest request, string? ipAddress)
    {
        // Get the session
        var session = await _context.Sessions
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId);

        if (session == null)
        {
            await LogScanAttemptAsync(request.SessionId, studentId, request.Token, 
                ScanResult.SessionInvalid, request.DeviceMetadata, ipAddress, "Session not found");
            // Persist failed scan attempt
            await _context.SaveChangesAsync();
            
            return new ScanResponse
            {
                Success = false,
                Result = ScanResult.SessionInvalid,
                Message = "Session not found"
            };
        }

        if (!session.IsActive)
        {
            await LogScanAttemptAsync(request.SessionId, studentId, request.Token,
                ScanResult.SessionInvalid, request.DeviceMetadata, ipAddress, "Session is not active");
            await _context.SaveChangesAsync();
            
            return new ScanResponse
            {
                Success = false,
                Result = ScanResult.SessionInvalid,
                Message = "Session is not active"
            };
        }

        // Check if student is enrolled
        var isEnrolled = await IsStudentEnrolledAsync(request.SessionId, studentId);
        if (!isEnrolled)
        {
            await LogScanAttemptAsync(request.SessionId, studentId, request.Token,
                ScanResult.NotEnrolled, request.DeviceMetadata, ipAddress, "Student not enrolled in course");
            await _context.SaveChangesAsync();
            
            return new ScanResponse
            {
                Success = false,
                Result = ScanResult.NotEnrolled,
                Message = "You are not enrolled in this course"
            };
        }

        // Check for duplicate scan
        var hasScranned = await HasStudentScannedAsync(request.SessionId, studentId);
        if (hasScranned)
        {
            await LogScanAttemptAsync(request.SessionId, studentId, request.Token,
                ScanResult.Duplicate, request.DeviceMetadata, ipAddress, "Duplicate scan attempt");
            await _context.SaveChangesAsync();
            
            return new ScanResponse
            {
                Success = false,
                Result = ScanResult.Duplicate,
                Message = "You have already marked your attendance for this session"
            };
        }

        // --- GEOFENCING CHECK ---
        if (session.Latitude.HasValue && session.Longitude.HasValue)
        {
            if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            {
                await LogScanAttemptAsync(request.SessionId, studentId, request.Token,
                    ScanResult.LocationRequired, request.DeviceMetadata, ipAddress, "Location required");
                await _context.SaveChangesAsync();

                return new ScanResponse
                {
                    Success = false,
                    Result = ScanResult.LocationRequired,
                    Message = "Location permission is required for this session."
                };
            }

            double distance = CalculateDistance(
                session.Latitude.Value, session.Longitude.Value,
                request.Latitude.Value, request.Longitude.Value);

            if (distance > session.MaxDistanceMeters)
            {
                await LogScanAttemptAsync(request.SessionId, studentId, request.Token,
                    ScanResult.LocationInvalid, request.DeviceMetadata, ipAddress, $"Too far: {distance:F1}m > {session.MaxDistanceMeters}m");
                await _context.SaveChangesAsync();

                return new ScanResponse
                {
                    Success = false,
                    Result = ScanResult.LocationInvalid,
                    Message = $"You are too far from the classroom ({distance:F0}m away)."
                };
            }
        }

        // Validate the QR token
        var tokenValidation = _qrEngine.ValidateToken(request.Token, request.SessionId);
        if (!tokenValidation.IsValid)
        {
            await LogScanAttemptAsync(request.SessionId, studentId, request.Token,
                tokenValidation.Result, request.DeviceMetadata, ipAddress, tokenValidation.ErrorMessage);
            await _context.SaveChangesAsync();
            
            return new ScanResponse
            {
                Success = false,
                Result = tokenValidation.Result,
                Message = tokenValidation.ErrorMessage ?? "Invalid token"
            };
        }

        // Determine attendance status based on time
        var now = DateTime.UtcNow;
        var status = now <= session.LateThresholdTime 
            ? AttendanceStatus.Present 
            : AttendanceStatus.Late;

        // Record attendance
        var attendanceRecord = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            StudentId = studentId,
            ScannedAt = now,
            Status = status,
            CreatedAt = now
        };

        _context.AttendanceRecords.Add(attendanceRecord);

        // Log successful scan
        await LogScanAttemptAsync(request.SessionId, studentId, request.Token,
            ScanResult.Success, request.DeviceMetadata, ipAddress, null);

        await _context.SaveChangesAsync();

        return new ScanResponse
        {
            Success = true,
            Result = ScanResult.Success,
            Message = status == AttendanceStatus.Present 
                ? "Attendance marked successfully!" 
                : "Attendance marked as LATE",
            AttendanceStatus = status,
            ScannedAt = now
        };
    }

    /// <summary>
    /// Gets attendance records for a session.
    /// </summary>
    public async Task<List<AttendanceRecordDto>> GetSessionAttendanceAsync(Guid sessionId)
    {
        return await _context.AttendanceRecords
            .Where(a => a.SessionId == sessionId)
            .Include(a => a.Student)
            .OrderByDescending(a => a.ScannedAt)
            .Select(a => new AttendanceRecordDto
            {
                StudentId = a.StudentId,
                StudentName = a.Student.FullName,
                UniversityId = a.Student.UniversityId,
                Status = a.Status,
                ScannedAt = a.ScannedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Gets real-time statistics for a session.
    /// </summary>
    public async Task<SessionStatsResponse> GetSessionStatsAsync(Guid sessionId)
    {
        var session = await _context.Sessions
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            throw new ArgumentException("Session not found", nameof(sessionId));

        // Get enrolled student count
        var totalEnrolled = await _context.CourseEnrollments
            .CountAsync(e => e.CourseId == session.CourseId && e.IsActive);

        // Get attendance counts by status
        var attendanceCounts = await _context.AttendanceRecords
            .Where(a => a.SessionId == sessionId)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var presentCount = attendanceCounts.FirstOrDefault(x => x.Status == AttendanceStatus.Present)?.Count ?? 0;
        var lateCount = attendanceCounts.FirstOrDefault(x => x.Status == AttendanceStatus.Late)?.Count ?? 0;
        var absentCount = attendanceCounts.FirstOrDefault(x => x.Status == AttendanceStatus.Absent)?.Count ?? 0;

        // Calculate pending (students who haven't scanned yet)
        var scannedCount = presentCount + lateCount;
        var pendingCount = session.IsActive ? totalEnrolled - scannedCount : 0;
        if (!session.IsActive)
        {
            absentCount = totalEnrolled - scannedCount;
        }

        // Get recent scans (last 5)
        var recentScans = await _context.AttendanceRecords
            .Where(a => a.SessionId == sessionId)
            .Include(a => a.Student)
            .OrderByDescending(a => a.ScannedAt)
            .Take(5)
            .Select(a => new AttendanceRecordDto
            {
                StudentId = a.StudentId,
                StudentName = a.Student.FullName,
                UniversityId = a.Student.UniversityId,
                Status = a.Status,
                ScannedAt = a.ScannedAt
            })
            .ToListAsync();

        var attendancePercentage = totalEnrolled > 0 
            ? ((double)(presentCount + lateCount) / totalEnrolled) * 100 
            : 0;

        return new SessionStatsResponse
        {
            SessionId = sessionId,
            SessionCode = session.SessionCode,
            IsActive = session.IsActive,
            StartTime = session.StartTime,
            TotalEnrolled = totalEnrolled,
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            PendingCount = pendingCount,
            AttendancePercentage = Math.Round(attendancePercentage, 1),
            RecentScans = recentScans
        };
    }

    /// <summary>
    /// Marks all students without attendance as absent when session closes.
    /// </summary>
    public async Task FinalizeSessionAttendanceAsync(Guid sessionId)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return;

        // Get all enrolled students who haven't scanned
        var scannedStudentIds = await _context.AttendanceRecords
            .Where(a => a.SessionId == sessionId)
            .Select(a => a.StudentId)
            .ToListAsync();

        var absentStudents = await _context.CourseEnrollments
            .Where(e => e.CourseId == session.CourseId 
                && e.IsActive 
                && !scannedStudentIds.Contains(e.StudentId))
            .Select(e => e.StudentId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var studentId in absentStudents)
        {
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                StudentId = studentId,
                ScannedAt = null,
                Status = AttendanceStatus.Absent,
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Marks a student as present (or late) manually by an instructor/admin.
    /// </summary>
    public async Task MarkStudentPresentAsync(Guid sessionId, string studentId, string markedBy)
    {
        // Get the session
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            throw new ArgumentException("Session not found", nameof(sessionId));

        // Check enrollment
        var isEnrolled = await IsStudentEnrolledAsync(sessionId, studentId);
        if (!isEnrolled)
            throw new InvalidOperationException("Student is not enrolled in the course for this session");

        // Check for existing attendance
        var hasScanned = await HasStudentScannedAsync(sessionId, studentId);
        if (hasScanned)
            throw new InvalidOperationException("Student already has an attendance record for this session");

        // Determine status based on session timeframe
        var now = DateTime.UtcNow;
        var status = now <= session.LateThresholdTime ? AttendanceStatus.Present : AttendanceStatus.Late;

        var attendanceRecord = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            StudentId = studentId,
            ScannedAt = now,
            Status = status,
            CreatedAt = now
        };

        _context.AttendanceRecords.Add(attendanceRecord);

        // Log the manual marking as a scan log for audit
        await LogScanAttemptAsync(sessionId, studentId, string.Empty,
            ScanResult.Success, null, null, $"Marked manually by {markedBy}");

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Unmarks a student's attendance record for the session (undo manual or scanned mark).
    /// </summary>
    public async Task UnmarkStudentAsync(Guid sessionId, string studentId, string markedBy)
    {
        // Find the attendance record
        var attendance = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.StudentId == studentId);

        if (attendance == null)
            throw new InvalidOperationException("No attendance record found for this student in the session");

        _context.AttendanceRecords.Remove(attendance);

        // Log the undo action
        await LogScanAttemptAsync(sessionId, studentId, string.Empty,
            ScanResult.Success, null, null, $"Unmarked by {markedBy}");

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Checks if a student is enrolled in the course for a session.
    /// </summary>
    public async Task<bool> IsStudentEnrolledAsync(Guid sessionId, string studentId)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return false;

        return await _context.CourseEnrollments
            .AnyAsync(e => e.CourseId == session.CourseId 
                && e.StudentId == studentId 
                && e.IsActive);
    }

    /// <summary>
    /// Checks if a student has already scanned for a session.
    /// </summary>
    public async Task<bool> HasStudentScannedAsync(Guid sessionId, string studentId)
    {
        return await _context.AttendanceRecords
            .AnyAsync(a => a.SessionId == sessionId && a.StudentId == studentId);
    }

    /// <summary>
    /// Logs a scan attempt for audit purposes.
    /// </summary>
    private Task LogScanAttemptAsync(
        Guid sessionId, 
        string studentId, 
        string token,
        ScanResult result, 
        string? deviceMetadata, 
        string? ipAddress,
        string? notes)
    {
        var scanLog = new ScanLog
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            StudentId = studentId,
            TokenUsed = token.Length > 500 ? token[..500] : token,
            ScanResult = result,
            DeviceMetadata = deviceMetadata?.Length > 1000 ? deviceMetadata[..1000] : deviceMetadata,
            IpAddress = ipAddress,
            ScannedAt = DateTime.UtcNow,
            Notes = notes
        };

        _context.ScanLogs.Add(scanLog);
        // Note: SaveChanges is called by the parent method
        return Task.CompletedTask;
    }

    /// <summary>
    /// Haversine formula to calculate distance between two coordinates in meters.
    /// </summary>
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3; // Earth radius in meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
