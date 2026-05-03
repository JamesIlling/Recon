using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LocationManagement.Api.Controllers;

/// <summary>
/// Provides user profile and configuration endpoints.
/// All endpoints require JWT authentication.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the authenticated user's profile (never exposes email).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user's profile DTO.</returns>
    /// <response code="200">User profile retrieved successfully.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("GetProfile called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        try
        {
            var profile = await _userService.GetProfileAsync(userId, ct);
            _logger.LogInformation("User profile retrieved for user {UserId}", userId);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("User profile requested for non-existent user {UserId}", userId);
            return NotFound(new { error = "User not found." });
        }
    }

    /// <summary>
    /// Changes the authenticated user's display name.
    /// </summary>
    /// <param name="request">The change display name request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user profile DTO.</returns>
    /// <response code="200">Display name changed successfully.</response>
    /// <response code="400">Validation error (empty display name, etc.).</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="404">User not found.</response>
    /// <response code="409">Display name already in use by another user.</response>
    [HttpPut("me/display-name")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeDisplayName(
        [FromBody] ChangeDisplayNameRequest request,
        CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.NewDisplayName))
        {
            return BadRequest(new { error = "Display name is required and cannot be empty." });
        }

        if (request.NewDisplayName.Length > 100)
        {
            return BadRequest(new { error = "Display name must not exceed 100 characters." });
        }

        var userId = GetAuthenticatedUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("ChangeDisplayName called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        try
        {
            var profile = await _userService.ChangeDisplayNameAsync(userId, request.NewDisplayName, ct);
            _logger.LogInformation("Display name changed for user {UserId}", userId);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Display name change requested for non-existent user {UserId}", userId);
            return NotFound(new { error = "User not found." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Display name change failed: {Message}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Changes the authenticated user's password.
    /// </summary>
    /// <param name="request">The change password request containing current and new passwords.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success message.</returns>
    /// <response code="200">Password changed successfully.</response>
    /// <response code="400">Validation error (weak password, etc.).</response>
    /// <response code="401">Unauthenticated request or incorrect current password.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { error = "Current password and new password are required." });
        }

        if (request.NewPassword.Length < 8)
        {
            return BadRequest(new { error = "New password must be at least 8 characters." });
        }

        var userId = GetAuthenticatedUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("ChangePassword called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        try
        {
            await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);
            _logger.LogInformation("Password changed for user {UserId}", userId);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Password change requested for non-existent user {UserId}", userId);
            return NotFound(new { error = "User not found." });
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Password change failed: incorrect current password for user {UserId}", userId);
            return Unauthorized(new { error = "Current password is incorrect." });
        }
    }

    /// <summary>
    /// Uploads a new avatar image for the authenticated user (1 MB limit, 1:1 crop).
    /// Replaces any previous avatar.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user profile DTO with the new avatar thumbnail URL.</returns>
    /// <response code="200">Avatar uploaded successfully.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="404">User not found.</response>
    /// <response code="413">File size exceeds 1 MB limit.</response>
    /// <response code="415">Unsupported media type (only JPEG, PNG, WebP allowed).</response>
    [HttpPut("me/avatar")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> UploadAvatar(CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("UploadAvatar called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        IFormFile? file = null;
        try
        {
            file = Request.Form.Files.FirstOrDefault();
        }
        catch (InvalidOperationException)
        {
            // Request doesn't have a proper form Content-Type header
            return BadRequest(new { error = "No file provided." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided." });
        }

        // Validate MIME type
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedMimeTypes.Contains(file.ContentType))
        {
            _logger.LogWarning("Avatar upload rejected: unsupported MIME type {MimeType} for user {UserId}", file.ContentType, userId);
            return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                new { error = "Unsupported media type. Only JPEG, PNG, and WebP are allowed." });
        }

        // Validate file size (1 MB limit)
        const long maxSizeBytes = 1_048_576; // 1 MB
        if (file.Length > maxSizeBytes)
        {
            _logger.LogWarning("Avatar upload rejected: file size {FileSize} exceeds 1 MB limit for user {UserId}", file.Length, userId);
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new { error = $"File size exceeds 1 MB limit. Received {file.Length} bytes." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var profile = await _userService.UploadAvatarAsync(userId, stream, file.ContentType, ct);
            _logger.LogInformation("Avatar uploaded for user {UserId}", userId);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Avatar upload requested for non-existent user {UserId}", userId);
            return NotFound(new { error = "User not found." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Avatar upload failed: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates the authenticated user's preferences (ShowPublicCollections flag).
    /// </summary>
    /// <param name="request">The update preferences request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user profile DTO.</returns>
    /// <response code="200">Preferences updated successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("me/preferences")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken ct)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        var userId = GetAuthenticatedUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("UpdatePreferences called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        try
        {
            var profile = await _userService.UpdatePreferencesAsync(userId, request.ShowPublicCollections, ct);
            _logger.LogInformation("Preferences updated for user {UserId}: ShowPublicCollections={ShowPublicCollections}",
                userId, request.ShowPublicCollections);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Preferences update requested for non-existent user {UserId}", userId);
            return NotFound(new { error = "User not found." });
        }
    }

    /// <summary>
    /// Extracts the authenticated user's ID from the JWT claims.
    /// </summary>
    /// <returns>The user ID, or Guid.Empty if not found.</returns>
    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }
}
