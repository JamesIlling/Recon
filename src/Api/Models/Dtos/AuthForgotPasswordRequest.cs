namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Request DTO for forgot password.
/// </summary>
public class AuthForgotPasswordRequest
{
    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public required string Email { get; set; }
}
