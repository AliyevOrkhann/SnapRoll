using SnapRoll.Domain.Enums;

namespace SnapRoll.Domain.Entities;

/// <summary>
/// Audit log for all QR code scan attempts.
/// Used for security analysis and fraud detection.
/// </summary>
public class ScanLog
{
    /// <summary>
    /// Unique identifier for the scan log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the session.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Navigation property to the session.
    /// </summary>
    public virtual Session Session { get; set; } = null!;

    /// <summary>
    /// Foreign key to the student who attempted the scan.
    /// </summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the student.
    /// </summary>
    public virtual AppUser Student { get; set; } = null!;

    /// <summary>
    /// The token that was used in the scan attempt.
    /// </summary>
    public string TokenUsed { get; set; } = string.Empty;

    /// <summary>
    /// The result of the scan attempt.
    /// </summary>
    public ScanResult ScanResult { get; set; }

    /// <summary>
    /// Device metadata from the scan (User-Agent, OS, etc.).
    /// </summary>
    public string? DeviceMetadata { get; set; }

    /// <summary>
    /// IP address of the device making the request.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Timestamp when the scan was attempted.
    /// </summary>
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional notes or error details.
    /// </summary>
    public string? Notes { get; set; }
}
