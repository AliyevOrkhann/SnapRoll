namespace SnapRoll.Infrastructure.Authentication;

/// <summary>
/// Configuration settings for JWT token generation and validation.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Secret key used to sign JWT tokens.
    /// Must be at least 32 characters (256 bits).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (typically the application name or URL).
    /// </summary>
    public string Issuer { get; set; } = "SnapRoll";

    /// <summary>
    /// Token audience (typically the client application).
    /// </summary>
    public string Audience { get; set; } = "SnapRollUsers";

    /// <summary>
    /// Token expiration time in minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
