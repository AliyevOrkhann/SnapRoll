using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;

namespace SnapRoll.API.Controllers;

/// <summary>
/// Controller for dashboard and statistics (Instructor only).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Instructor,Admin")]
public class DashboardController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ISessionService _sessionService;

    public DashboardController(
        IAttendanceService attendanceService,
        ISessionService sessionService)
    {
        _attendanceService = attendanceService;
        _sessionService = sessionService;
    }

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    /// <summary>
    /// Gets real-time attendance statistics for a session.
    /// </summary>
    [HttpGet("{sessionId:guid}/stats")]
    [ProducesResponseType(typeof(SessionStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionStatsResponse>> GetSessionStats(Guid sessionId)
    {
        // Verify instructor owns this session
        var isInstructor = await _sessionService.IsSessionInstructorAsync(sessionId, UserId);
        if (!isInstructor)
        {
            return Unauthorized(new { message = "You are not the instructor for this session" });
        }

        try
        {
            var stats = await _attendanceService.GetSessionStatsAsync(sessionId);
            return Ok(stats);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets attendance records for a session.
    /// </summary>
    [HttpGet("{sessionId:guid}/attendance")]
    [ProducesResponseType(typeof(List<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<AttendanceRecordDto>>> GetAttendance(Guid sessionId)
    {
        // Verify instructor owns this session
        var isInstructor = await _sessionService.IsSessionInstructorAsync(sessionId, UserId);
        if (!isInstructor)
        {
            return Unauthorized(new { message = "You are not the instructor for this session" });
        }

        var records = await _attendanceService.GetSessionAttendanceAsync(sessionId);
        return Ok(records);
    }
}
