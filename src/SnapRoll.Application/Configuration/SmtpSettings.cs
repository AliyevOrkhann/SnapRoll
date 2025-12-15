namespace SnapRoll.Application.Configuration;

/// <summary>
/// SMTP email configuration settings.
/// </summary>
public class SmtpSettings
{
    public const string SectionName = "SmtpSettings";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Username for SMTP authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for SMTP authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Sender email address.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Sender display name.
    /// </summary>
    public string FromName { get; set; } = "SnapRoll";

    /// <summary>
    /// Enable SSL/TLS for SMTP connection.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Frontend base URL for verification links.
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "https://snaproll.onrender.com";
}
