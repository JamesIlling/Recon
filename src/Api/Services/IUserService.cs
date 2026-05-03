using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Services;

/// <summary>
/// DTO for user profile response (never exposes email publicly).
/// </summary>
public sealed record UserProfileDto(
    Guid Id,
    string Username,
    string DisplayName,
    string? AvatarThumbnailUrl,
    bool ShowPublicCollections,
    DateTimeOffset CreatedAt
);

/// <summary>
/// Service interface for user profile and configuration operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves the authenticated user's profile (never exposes email).
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user's profile DTO.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Changes the user's display name with uniqueness validation (case-insensitive).
    /// Records an AuditEvent for the change.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="newDisplayName">The new display name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user profile DTO.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the display name is already in use by another user.</exception>
    Task<UserProfileDto> ChangeDisplayNameAsync(Guid userId, string newDisplayName, CancellationToken ct);

    /// <summary>
    /// Changes the user's password after verifying the current password.
    /// Records an AuditEvent for the change.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="currentPassword">The user's current password (plaintext).</param>
    /// <param name="newPassword">The new password (plaintext).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the current password is incorrect.</exception>
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct);

    /// <summary>
    /// Uploads a new avatar image for the user (1 MB limit, 1:1 crop, ThumbnailVariant only).
    /// Replaces any previous avatar. Records an AuditEvent for the upload.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="imageStream">The image file stream.</param>
    /// <param name="mimeType">The MIME type of the image.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user profile DTO with the new avatar thumbnail URL.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the image exceeds 1 MB or processing fails.</exception>
    Task<UserProfileDto> UploadAvatarAsync(Guid userId, Stream imageStream, string mimeType, CancellationToken ct);

    /// <summary>
    /// Updates the user's preferences (ShowPublicCollections flag).
    /// Records an AuditEvent for the change.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="showPublicCollections">Whether to show public collections on the user's homepage.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user profile DTO.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    Task<UserProfileDto> UpdatePreferencesAsync(Guid userId, bool showPublicCollections, CancellationToken ct);
}
