using SnapRoll.Domain.Enums;

namespace SnapRoll.Application.DTOs;

/// <summary>
/// Represents a generated QR token payload.
/// </summary>
public class QrTokenPayload
{
    /// <summary>
    /// The full token string to be encoded in the QR code.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Session ID this token is valid for.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Timestamp when the token was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Timestamp when the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Sequence number for this token in the session (for debugging).
    /// </summary>
    public long Sequence { get; set; }
}

/// <summary>
/// Result of validating a QR token.
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Whether the token is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The result code for the validation.
    /// </summary>
    public ScanResult Result { get; set; }

    /// <summary>
    /// Session ID extracted from the token.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Token generation timestamp.
    /// </summary>
    public DateTime? TokenTimestamp { get; set; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    public static TokenValidationResult Success(Guid sessionId, DateTime timestamp) => new()
    {
        IsValid = true,
        Result = ScanResult.Success,
        SessionId = sessionId,
        TokenTimestamp = timestamp
    };

    public static TokenValidationResult Failure(ScanResult result, string message) => new()
    {
        IsValid = false,
        Result = result,
        ErrorMessage = message
    };
}
