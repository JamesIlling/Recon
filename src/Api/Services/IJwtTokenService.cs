namespace LocationManagement.Api.Services;

/// <summary>
/// Issues and validates JWT bearer tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Issues a new JWT bearer token for a user.
    /// </summary>
    /// <param name="userId">The user ID to include in the token.</param>
    /// <param name="userRole">The user's role (e.g., "Standard", "Admin").</param>
    /// <returns>A JWT bearer token string.</returns>
    string IssueToken(Guid userId, string userRole);

    /// <summary>
    /// Validates a JWT bearer token and extracts the user ID.
    /// </summary>
    /// <param name="token">The JWT token string (without "Bearer " prefix).</param>
    /// <returns>The user ID from the token, or null if validation fails.</returns>
    Guid? ValidateToken(string token);
}
