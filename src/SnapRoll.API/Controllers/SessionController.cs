using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SnapRoll.API.Hubs;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;

namespace SnapRoll.API.Controllers;

/// <summary>
/// Controller for session management (Instructor only).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Instructor,Admin")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IQrEngine _qrEngine;
    private readonly IHubContext<SessionHub> _hubContext;

    public SessionController(
        ISessionService sessionService,
        IQrEngine qrEngine,
        IHubContext<SessionHub> hubContext)
    {
        _sessionService = sessionService;
        _qrEngine = qrEngine;
        _hubContext = hubContext;
    }

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    /// <summary>
    /// Creates a new attendance session for a course.
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SessionResponse>> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            var session = await _sessionService.CreateSessionAsync(UserId, request);
            return Ok(session);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current valid QR token for a session.
    /// </summary>
    [HttpGet("{sessionId:guid}/qr-token")]
    [ProducesResponseType(typeof(QrTokenPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QrTokenPayload>> GetQrToken(Guid sessionId)
    {
        // Verify instructor owns this session
        var isInstructor = await _sessionService.IsSessionInstructorAsync(sessionId, UserId);
        if (!isInstructor)
        {
            return Unauthorized(new { message = "You are not the instructor for this session" });
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Session not found" });
        }

        if (!session.IsActive)
        {
            return BadRequest(new { message = "Session is not active" });
        }

        var token = _qrEngine.GenerateToken(sessionId);
        return Ok(token);
    }

    /// <summary>
    /// Closes an active session.
    /// </summary>
    [HttpPost("{sessionId:guid}/close")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SessionResponse>> CloseSession(Guid sessionId)
    {
        try
        {
            var session = await _sessionService.CloseSessionAsync(sessionId, UserId);
            
            // Notify connected clients that session has closed
            await _hubContext.Clients.Group($"session-{sessionId}")
                .SendAsync("SessionClosed", session);

            return Ok(session);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Exports attendance data as a CSV file.
    /// </summary>
    [HttpGet("{sessionId:guid}/report")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportReport(Guid sessionId)
    {
        try
        {
            var csvData = await _sessionService.ExportAttendanceCsvAsync(sessionId, UserId);
            var session = await _sessionService.GetSessionAsync(sessionId);
            var fileName = $"attendance_{session?.SessionCode ?? sessionId.ToString()}_{DateTime.UtcNow:yyyyMMdd}.csv";
            
            return File(csvData, "text/csv", fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets session details.
    /// </summary>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> GetSession(Guid sessionId)
    {
        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Session not found" });
        }

        return Ok(session);
    }

    /// <summary>
    /// Manually mark a student as present for a session (Instructor/Admin).
    /// </summary>
    [HttpPost("{sessionId:guid}/mark-student")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkStudentPresent(Guid sessionId, [FromBody] MarkStudentRequest request)
    {
        try
        {
            // Use attendance service to mark the student
            var attendanceService = HttpContext.RequestServices.GetService(typeof(SnapRoll.Application.Interfaces.IAttendanceService)) as SnapRoll.Application.Interfaces.IAttendanceService;
            if (attendanceService == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Attendance service not available" });

            await attendanceService.MarkStudentPresentAsync(sessionId, request.StudentId, UserId);

            // After marking, notify connected clients about updated stats
            var stats = await attendanceService.GetSessionStatsAsync(sessionId);
            await SessionHub.NotifyAttendanceUpdateAsync(_hubContext, sessionId, stats);

            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Unmark a student's attendance for a session (Instructor/Admin).
    /// </summary>
    [HttpPost("{sessionId:guid}/unmark-student")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnmarkStudent(Guid sessionId, [FromBody] MarkStudentRequest request)
    {
        try
        {
            var attendanceService = HttpContext.RequestServices.GetService(typeof(SnapRoll.Application.Interfaces.IAttendanceService)) as SnapRoll.Application.Interfaces.IAttendanceService;
            if (attendanceService == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Attendance service not available" });

            await attendanceService.UnmarkStudentAsync(sessionId, request.StudentId, UserId);

            var stats = await attendanceService.GetSessionStatsAsync(sessionId);
            await SessionHub.NotifyAttendanceUpdateAsync(_hubContext, sessionId, stats);

            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets historical sessions for a course (instructor only).
    /// </summary>
    [HttpGet("course/{courseId:guid}/history")]
    [ProducesResponseType(typeof(List<SessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<SessionResponse>>> GetCourseSessionsHistory(Guid courseId)
    {
        try
        {
            var sessions = await _sessionService.GetCourseSessionsForInstructorAsync(courseId, UserId);
            return Ok(sessions);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets all active sessions for the current instructor.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<SessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SessionResponse>>> GetActiveSessions()
    {
        var sessions = await _sessionService.GetActiveSessionsAsync(UserId);
        return Ok(sessions);
    }
}
