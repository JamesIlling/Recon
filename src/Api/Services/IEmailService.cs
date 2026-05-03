namespace LocationManagement.Api.Services;

/// <summary>
/// Sends emails via SMTP.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email message.
    /// </summary>
    /// <param name="toAddress">The recipient email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body (plain text or HTML).</param>
    /// <param name="isHtml">Whether the body is HTML (true) or plain text (false).</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync(string toAddress, string subject, string body, bool isHtml, CancellationToken ct);

    /// <summary>
    /// Sends a password reset email with a reset token.
    /// </summary>
    /// <param name="toAddress">The recipient email address.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="resetToken">The password reset token (plaintext).</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendPasswordResetEmailAsync(string toAddress, string displayName, string resetToken, CancellationToken ct);
}
