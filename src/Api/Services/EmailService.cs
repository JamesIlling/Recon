using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace LocationManagement.Api.Services;

/// <summary>
/// Sends emails via SMTP.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUsername;
    private readonly string? _smtpPassword;
    private readonly string? _fromAddress;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// Reads SMTP configuration from environment variables.
    /// </summary>
    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
        _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
        _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
        _smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME");
        _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        _fromAddress = Environment.GetEnvironmentVariable("SMTP_FROM_ADDRESS");

        if (string.IsNullOrEmpty(_smtpHost) || string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword) || string.IsNullOrEmpty(_fromAddress))
        {
            _logger.LogCritical("SMTP configuration is incomplete. Set SMTP_HOST, SMTP_PORT, SMTP_USERNAME, SMTP_PASSWORD, and SMTP_FROM_ADDRESS environment variables.");
        }
    }

    /// <summary>
    /// Sends an email message.
    /// </summary>
    public async Task SendAsync(string toAddress, string subject, string body, bool isHtml, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_smtpHost) || string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword) || string.IsNullOrEmpty(_fromAddress))
        {
            throw new InvalidOperationException("SMTP configuration is not properly configured.");
        }

        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
            EnableSsl = true
        };

        using var message = new MailMessage(_fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        try
        {
            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Email sent to {ToAddress} with subject {Subject}", toAddress, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToAddress}", toAddress);
            throw;
        }
    }

    /// <summary>
    /// Sends a password reset email with a reset token.
    /// </summary>
    public async Task SendPasswordResetEmailAsync(string toAddress, string displayName, string resetToken, CancellationToken ct)
    {
        var subject = "Password Reset Request";
        var body = $@"Hello {displayName},

You have requested to reset your password. Please use the following link to reset your password:

Reset Token: {resetToken}

This link will expire in 1 hour.

If you did not request this password reset, please ignore this email.

Best regards,
Location Management Team";

        await SendAsync(toAddress, subject, body, isHtml: false, ct);
    }
}
