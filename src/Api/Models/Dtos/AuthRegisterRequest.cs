namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Request DTO for user registration.
/// </summary>
public class AuthRegisterRequest
{
    /// <summary>
    /// Gets or sets the username (login identifier).
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the display name shown to other users.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the plaintext password.
    /// </summary>
    public required string Password { get; set; }
}
