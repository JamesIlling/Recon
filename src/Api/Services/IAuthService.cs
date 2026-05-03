namespace LocationManagement.Api.Services;

/// <summary>
/// Provides authentication operations: registration, login, password reset, and password change.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user with the provided credentials.
    /// Validates uniqueness of username, display name, and email.
    /// Hashes the password with BCrypt (cost 12).
    /// Assigns Standard role to all new users; the first user is promoted to Admin.
    /// </summary>
    /// <param name="username">The username (max 50 chars, must be unique).</param>
    /// <param name="displayName">The display name (max 100 chars, must be unique).</param>
    /// <param name="email">The email address (must be unique and valid).</param>
    /// <param name="password">The plaintext password (must meet complexity rules).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created user ID and a JWT token.</returns>
    /// <exception cref="InvalidOperationException">Thrown when username, display name, or email already exists.</exception>
    /// <exception cref="ArgumentException">Thrown when input validation fails.</exception>
    Task<(Guid UserId, string Jwt)> RegisterAsync(
        string username,
        string displayName,
        string email,
        string password,
        CancellationToken ct);

    /// <summary>
    /// Authenticates a user with username and password.
    /// Returns a JWT token on success.
    /// Records a failed authentication audit event on failure.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The plaintext password.</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user ID, display name, and JWT token on success.</returns>
    /// <exception cref="InvalidOperationException">Thrown when credentials are invalid.</exception>
    Task<(Guid UserId, string DisplayName, string Jwt)> LoginAsync(
        string username,
        string password,
        string sourceIp,
        CancellationToken ct);

    /// <summary>
    /// Initiates a password reset by generating a single-use token and sending a reset email.
    /// Returns the same response for unknown usernames to prevent user enumeration.
    /// </summary>
    /// <param name="email">The email address associated with the user account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A success message (same for known and unknown emails).</returns>
    Task<string> ForgotPasswordAsync(string email, CancellationToken ct);

    /// <summary>
    /// Completes a password reset using a single-use token.
    /// Validates the token, updates the password hash, and invalidates the token.
    /// </summary>
    /// <param name="token">The plaintext reset token (will be hashed and compared).</param>
    /// <param name="newPassword">The new plaintext password (must meet complexity rules).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A success message.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the token is invalid, expired, or already used.</exception>
    /// <exception cref="ArgumentException">Thrown when the new password fails validation.</exception>
    Task<string> ResetPasswordAsync(string token, string newPassword, CancellationToken ct);

    /// <summary>
    /// Changes the password for an authenticated user.
    /// Verifies the current password before updating.
    /// </summary>
    /// <param name="userId">The ID of the user changing their password.</param>
    /// <param name="currentPassword">The current plaintext password.</param>
    /// <param name="newPassword">The new plaintext password (must meet complexity rules).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A success message.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current password is incorrect.</exception>
    /// <exception cref="ArgumentException">Thrown when the new password fails validation.</exception>
    Task<string> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct);
}
