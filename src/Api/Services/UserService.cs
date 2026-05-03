using Microsoft.Extensions.Logging;
using BCrypt.Net;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Repositories;

namespace LocationManagement.Api.Services;

/// <summary>
/// Service implementation for user profile and configuration operations.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserService> _logger;

    private const long AvatarMaxSizeBytes = 1_048_576; // 1 MB

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="imageProcessingService">The image processing service.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="logger">The logger.</param>
    public UserService(
        IUserRepository userRepository,
        IImageProcessingService imageProcessingService,
        IAuditService auditService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves the authenticated user's profile (never exposes email).
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user's profile DTO.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            _logger.LogWarning("User profile requested for non-existent user {UserId}", userId);
            throw new KeyNotFoundException($"User {userId} not found.");
        }

        _logger.LogInformation("User profile retrieved for user {UserId}", userId);

        return new UserProfileDto(
            user.Id,
            user.Username,
            user.DisplayName,
            user.AvatarImage?.ThumbnailUrl,
            user.ShowPublicCollections,
            user.CreatedAt
        );
    }

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
    public async Task<UserProfileDto> ChangeDisplayNameAsync(Guid userId, string newDisplayName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newDisplayName);

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            _logger.LogWarning("Display name change requested for non-existent user {UserId}", userId);
            throw new KeyNotFoundException($"User {userId} not found.");
        }

        // Check if the new display name is already in use by another user (case-insensitive)
        var isInUse = await _userRepository.IsDisplayNameInUseAsync(newDisplayName, userId, ct);
        if (isInUse)
        {
            _logger.LogWarning("Display name change failed: display name '{DisplayName}' already in use for user {UserId}", newDisplayName, userId);
            await _auditService.RecordAsync(
                "DisplayNameChangeAttempted",
                userId,
                nameof(User),
                userId,
                AuditOutcome.Failure,
                string.Empty,
                ct);
            throw new InvalidOperationException($"Display name '{newDisplayName}' is already in use.");
        }

        user.DisplayName = newDisplayName;
        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation("Display name changed for user {UserId}", userId);
        await _auditService.RecordAsync(
            "DisplayNameChanged",
            userId,
            nameof(User),
            userId,
            AuditOutcome.Success,
            string.Empty,
            ct);

        return new UserProfileDto(
            user.Id,
            user.Username,
            user.DisplayName,
            user.AvatarImage?.ThumbnailUrl,
            user.ShowPublicCollections,
            user.CreatedAt
        );
    }

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
    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            _logger.LogWarning("Password change requested for non-existent user {UserId}", userId);
            throw new KeyNotFoundException($"User {userId} not found.");
        }

        // Verify current password using BCrypt
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password change failed: incorrect current password for user {UserId}", userId);
            await _auditService.RecordAsync(
                "PasswordChangeAttempted",
                userId,
                nameof(User),
                userId,
                AuditOutcome.Failure,
                string.Empty,
                ct);
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        // Hash the new password with BCrypt (cost factor 12)
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);
        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation("Password changed for user {UserId}", userId);
        await _auditService.RecordAsync(
            "PasswordChanged",
            userId,
            nameof(User),
            userId,
            AuditOutcome.Success,
            string.Empty,
            ct);
    }

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
    public async Task<UserProfileDto> UploadAvatarAsync(Guid userId, Stream imageStream, string mimeType, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(imageStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            _logger.LogWarning("Avatar upload requested for non-existent user {UserId}", userId);
            throw new KeyNotFoundException($"User {userId} not found.");
        }

        // Validate file size (1 MB limit)
        if (imageStream.Length > AvatarMaxSizeBytes)
        {
            _logger.LogWarning("Avatar upload failed: file size {FileSize} exceeds 1 MB limit for user {UserId}", imageStream.Length, userId);
            await _auditService.RecordAsync(
                "AvatarUploadAttempted",
                userId,
                nameof(User),
                userId,
                AuditOutcome.Failure,
                string.Empty,
                ct);
            throw new InvalidOperationException($"Avatar image must not exceed 1 MB. Received {imageStream.Length} bytes.");
        }

        // Process and store the avatar (generates ThumbnailVariant only)
        var variants = await _imageProcessingService.ProcessAndStoreAsync(imageStream, mimeType, null, ct);

        // Delete previous avatar if it exists
        if (user.AvatarImageId.HasValue)
        {
            try
            {
                await _imageProcessingService.DeleteImageAndVariantsAsync(user.AvatarImageId.Value, ct);
                _logger.LogInformation("Previous avatar deleted for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete previous avatar for user {UserId}", userId);
            }
        }

        // Update user with new avatar image ID
        user.AvatarImageId = variants.ImageId;
        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation("Avatar uploaded for user {UserId}", userId);
        await _auditService.RecordAsync(
            "AvatarUploaded",
            userId,
            nameof(User),
            userId,
            AuditOutcome.Success,
            string.Empty,
            ct);

        return new UserProfileDto(
            user.Id,
            user.Username,
            user.DisplayName,
            variants.ThumbnailUrl,
            user.ShowPublicCollections,
            user.CreatedAt
        );
    }

    /// <summary>
    /// Updates the user's preferences (ShowPublicCollections flag).
    /// Records an AuditEvent for the change.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="showPublicCollections">Whether to show public collections on the user's homepage.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user profile DTO.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    public async Task<UserProfileDto> UpdatePreferencesAsync(Guid userId, bool showPublicCollections, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            _logger.LogWarning("Preferences update requested for non-existent user {UserId}", userId);
            throw new KeyNotFoundException($"User {userId} not found.");
        }

        user.ShowPublicCollections = showPublicCollections;
        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation("Preferences updated for user {UserId}: ShowPublicCollections={ShowPublicCollections}", userId, showPublicCollections);
        await _auditService.RecordAsync(
            "PreferencesUpdated",
            userId,
            nameof(User),
            userId,
            AuditOutcome.Success,
            string.Empty,
            ct);

        return new UserProfileDto(
            user.Id,
            user.Username,
            user.DisplayName,
            user.AvatarImage?.ThumbnailUrl,
            user.ShowPublicCollections,
            user.CreatedAt
        );
    }
}
