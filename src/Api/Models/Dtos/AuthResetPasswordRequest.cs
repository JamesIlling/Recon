namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Request DTO for password reset.
/// </summary>
public class AuthResetPasswordRequest
{
    /// <summary>
    /// Gets or sets the password reset token.
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Gets or sets the new plaintext password.
    /// </summary>
    public required string NewPassword { get; set; }
}
