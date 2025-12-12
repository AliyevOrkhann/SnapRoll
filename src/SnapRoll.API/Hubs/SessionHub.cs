using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;

namespace SnapRoll.API.Hubs;

/// <summary>
/// SignalR hub for real-time session management.
/// Handles QR token streaming and attendance update notifications.
/// </summary>
[Authorize]
public class SessionHub : Hub
{
    private readonly IQrEngine _qrEngine;
    private readonly ISessionService _sessionService;
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<SessionHub> _logger;

    public SessionHub(
        IQrEngine qrEngine,
        ISessionService sessionService,
        IAttendanceService attendanceService,
        ILogger<SessionHub> logger)
    {
        _qrEngine = qrEngine;
        _sessionService = sessionService;
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// Instructor joins a session room to receive updates.
    /// </summary>
    public async Task JoinSession(Guid sessionId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User not authenticated");
        }

        // Verify user is the instructor for this session
        var isInstructor = await _sessionService.IsSessionInstructorAsync(sessionId, userId);
        if (!isInstructor)
        {
            throw new HubException("You are not authorized to join this session");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
        _logger.LogInformation("Instructor {UserId} joined session {SessionId}", userId, sessionId);

        // Send initial stats
        var stats = await _attendanceService.GetSessionStatsAsync(sessionId);
        await Clients.Caller.SendAsync("InitialStats", stats);
    }

    /// <summary>
    /// Instructor leaves a session room.
    /// </summary>
    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
        _logger.LogInformation("User left session {SessionId}", sessionId);
    }

    /// <summary>
    /// Streams QR tokens to the instructor client every 2 seconds.
    /// Uses async enumerable for efficient streaming.
    /// </summary>
    public async IAsyncEnumerable<QrTokenPayload> StreamQrToken(
        Guid sessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User not authenticated");
        }

        // Verify user is the instructor
        var isInstructor = await _sessionService.IsSessionInstructorAsync(sessionId, userId);
        if (!isInstructor)
        {
            throw new HubException("You are not authorized to stream tokens for this session");
        }

        _logger.LogInformation("Starting QR token stream for session {SessionId}", sessionId);

        while (!cancellationToken.IsCancellationRequested)
        {
            // Check if session is still active
            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null || !session.IsActive)
            {
                _logger.LogInformation("Session {SessionId} is no longer active, stopping stream", sessionId);
                yield break;
            }

            // Generate new token
            var token = _qrEngine.GenerateToken(sessionId);
            yield return token;

            // Wait 2 seconds before generating next token
            try
            {
                await Task.Delay(2000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("QR token stream ended for session {SessionId}", sessionId);
    }

    /// <summary>
    /// Student joins a session room (optional, for future features).
    /// </summary>
    public async Task JoinSessionAsStudent(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-students-{sessionId}");
    }

    /// <summary>
    /// Notifies all clients in a session about an attendance update.
    /// Called from AttendanceService after successful scan.
    /// </summary>
    public static async Task NotifyAttendanceUpdateAsync(
        IHubContext<SessionHub> hubContext,
        Guid sessionId,
        SessionStatsResponse stats)
    {
        await hubContext.Clients.Group($"session-{sessionId}")
            .SendAsync("AttendanceUpdated", stats);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
