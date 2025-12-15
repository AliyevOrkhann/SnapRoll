using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapRoll.Application.Configuration;
using SnapRoll.Application.Interfaces;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace SnapRoll.Infrastructure.Services;

/// <summary>
/// SMTP-based email service implementation using MailKit.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sends an email using SMTP.
    /// </summary>
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Accept all SSL certificates (in case of self-signed servers or proxy issues, though Brevo should be fine)
            // client.ServerCertificateValidationCallback = (s, c, h, e) => true; 
            
            // Connect to the server
            // We use Auto which tries to negotiate the best options
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);

            // Authenticate
            await client.AuthenticateAsync(_settings.Username, _settings.Password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }
}
