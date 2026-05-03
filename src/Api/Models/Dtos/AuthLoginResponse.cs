namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Response DTO for user login.
/// </summary>
public class AuthLoginResponse
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the JWT bearer token.
    /// </summary>
    public required string Jwt { get; set; }
}
