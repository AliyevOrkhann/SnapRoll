namespace SnapRoll.Infrastructure.Authentication;

/// <summary>
/// Configuration settings for QR token generation and validation.
/// </summary>
public class QrSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "QrSettings";

    /// <summary>
    /// Time-to-live for QR tokens in seconds.
    /// Tokens older than this will be rejected.
    /// </summary>
    public int TokenTtlSeconds { get; set; } = 2;

    /// <summary>
    /// Buffer time in seconds to account for network latency.
    /// Total valid window = TokenTtlSeconds + ValidationBufferSeconds.
    /// </summary>
    public int ValidationBufferSeconds { get; set; } = 2;

    /// <summary>
    /// Secret key used to sign QR tokens.
    /// Must be at least 32 characters (256 bits).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Total valid time window for token acceptance.
    /// </summary>
    public int TotalValidWindowSeconds => TokenTtlSeconds + ValidationBufferSeconds;
}
