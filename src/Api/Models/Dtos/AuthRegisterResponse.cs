namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Response DTO for user registration.
/// </summary>
public class AuthRegisterResponse
{
    /// <summary>
    /// Gets or sets the newly created user ID.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the JWT bearer token.
    /// </summary>
    public required string Jwt { get; set; }
}
