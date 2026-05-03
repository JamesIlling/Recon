namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents a single-use password reset token issued to a user.
/// Tokens expire after 1 hour and are stored as SHA-256 hashes.
/// </summary>
public class PasswordResetToken
{
    /// <summary>
    /// Gets or sets the unique identifier for the password reset token.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the user for whom this token was issued.
    /// Foreign key to User.Id.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the actual token.
    /// The plaintext token is never persisted; only the hash is stored.
    /// </summary>
    public required string TokenHash { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this token expires.
    /// Tokens are valid for 1 hour from issuance.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this token has been used.
    /// Tokens are single-use; once used, they cannot be reused.
    /// Defaults to false.
    /// </summary>
    public required bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the token was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the navigation property for the user associated with this token.
    /// </summary>
    public virtual User User { get; set; } = null!;
}
