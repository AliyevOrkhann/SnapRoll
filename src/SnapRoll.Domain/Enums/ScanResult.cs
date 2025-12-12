namespace SnapRoll.Domain.Enums;

/// <summary>
/// Defines the result of a QR code scan attempt.
/// </summary>
public enum ScanResult
{
    /// <summary>
    /// Token was valid and attendance was recorded.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Token has expired (outside the valid time window).
    /// </summary>
    Expired = 1,

    /// <summary>
    /// Student has already scanned for this session.
    /// </summary>
    Duplicate = 2,

    /// <summary>
    /// Token signature is invalid or token is malformed.
    /// </summary>
    Invalid = 3,

    /// <summary>
    /// Session is not active or does not exist.
    /// </summary>
    SessionInvalid = 4,

    /// <summary>
    /// Student is not enrolled in the course.
    /// </summary>
    NotEnrolled = 5
}
