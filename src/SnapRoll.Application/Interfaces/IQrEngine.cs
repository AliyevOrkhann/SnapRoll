using SnapRoll.Application.DTOs;

namespace SnapRoll.Application.Interfaces;

/// <summary>
/// Interface for the QR token generation and validation engine.
/// Handles cryptographically secure, time-based token rotation.
/// </summary>
public interface IQrEngine
{
    /// <summary>
    /// Generates a new cryptographically secure QR token for a session.
    /// Tokens rotate every 2 seconds.
    /// </summary>
    /// <param name="sessionId">The session ID to generate the token for.</param>
    /// <returns>A QrTokenPayload containing the token and metadata.</returns>
    QrTokenPayload GenerateToken(Guid sessionId);

    /// <summary>
    /// Validates a scanned QR token.
    /// Checks signature authenticity, TTL, and session validity.
    /// </summary>
    /// <param name="token">The token string from the QR code.</param>
    /// <param name="expectedSessionId">The session ID the student is trying to scan for.</param>
    /// <returns>Validation result with success/failure details.</returns>
    TokenValidationResult ValidateToken(string token, Guid expectedSessionId);

    /// <summary>
    /// Gets the current valid token for a session without generating a new one.
    /// Returns null if no current token exists.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>Current token payload or null.</returns>
    QrTokenPayload? GetCurrentToken(Guid sessionId);
}
