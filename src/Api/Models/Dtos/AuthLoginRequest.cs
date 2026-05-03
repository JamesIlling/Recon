namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Request DTO for user login.
/// </summary>
public class AuthLoginRequest
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the plaintext password.
    /// </summary>
    public required string Password { get; set; }
}
