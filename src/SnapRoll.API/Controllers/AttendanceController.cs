using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SnapRoll.API.Hubs;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;

namespace SnapRoll.API.Controllers;

/// <summary>
/// Controller for attendance operations (Student only).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IHubContext<SessionHub> _hubContext;

    public AttendanceController(
        IAttendanceService attendanceService,
        IHubContext<SessionHub> hubContext)
    {
        _attendanceService = attendanceService;
        _hubContext = hubContext;
    }

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    /// <summary>
    /// Processes a QR code scan to mark attendance.
    /// </summary>
    /// <param name="request">Scan request with session ID and token.</param>
    /// <returns>Scan result indicating success or failure.</returns>
    [HttpPost("scan")]
    [ProducesResponseType(typeof(ScanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ScanResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScanResponse>> Scan([FromBody] ScanRequest request)
    {
        // Get client IP address
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        // Add User-Agent to device metadata if not provided
        if (string.IsNullOrEmpty(request.DeviceMetadata))
        {
            request.DeviceMetadata = Request.Headers.UserAgent.ToString();
        }

        var result = await _attendanceService.ProcessScanAsync(UserId, request, ipAddress);

        // If successful, notify the instructor's dashboard
        if (result.Success)
        {
            var stats = await _attendanceService.GetSessionStatsAsync(request.SessionId);
            await SessionHub.NotifyAttendanceUpdateAsync(_hubContext, request.SessionId, stats);
        }

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets attendance history for the current student in a specific course.
    /// </summary>
    [HttpGet("my-history/{courseId:guid}")]
    [ProducesResponseType(typeof(List<StudentAttendanceHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StudentAttendanceHistoryDto>>> GetMyHistory(Guid courseId)
    {
        var history = await _attendanceService.GetStudentAttendanceHistoryAsync(UserId, courseId);
        return Ok(history);
    }
}
