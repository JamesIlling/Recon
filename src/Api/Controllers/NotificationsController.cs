using LocationManagement.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LocationManagement.Api.Controllers;

/// <summary>
/// Provides in-app notification management endpoints.
/// All endpoints require JWT authentication.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationRepository notificationRepository,
        ILogger<NotificationsController> logger)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all notifications for the authenticated user.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of notifications for the user.</returns>
    /// <response code="200">Notifications retrieved successfully.</response>
    /// <response code="401">Unauthenticated request.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("GetNotifications called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        try
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId, ct);
            var dtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Message = n.Message,
                RelatedResourceId = n.RelatedResourceId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();

            _logger.LogInformation("Retrieved {NotificationCount} notifications for user {UserId}", dtos.Count, userId);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving notifications." });
        }
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="id">The notification ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Notification marked as read successfully.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="404">Notification not found.</response>
    [HttpPut("{id}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("MarkAsRead called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        try
        {
            var notification = await _notificationRepository.GetByIdAsync(id, ct);
            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", id, userId);
                return NotFound(new { error = "Notification not found." });
            }

            // Verify the notification belongs to the authenticated user
            if (notification.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to mark notification {NotificationId} belonging to user {OwnerId} as read",
                    userId, id, notification.UserId);
                return NotFound(new { error = "Notification not found." });
            }

            // Mark as read
            notification.IsRead = true;
            await _notificationRepository.UpdateAsync(notification, ct);

            _logger.LogInformation("Notification {NotificationId} marked as read for user {UserId}", id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", id, userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the notification." });
        }
    }

    /// <summary>
    /// Marks all notifications as read for the authenticated user.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">All notifications marked as read successfully.</response>
    /// <response code="401">Unauthenticated request.</response>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("MarkAllAsRead called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        try
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId, ct);

            var unreadNotifications = notifications.Where(n => !n.IsRead).ToList();
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                await _notificationRepository.UpdateAsync(notification, ct);
            }

            _logger.LogInformation("All {UnreadCount} notifications marked as read for user {UserId}", unreadNotifications.Count, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating notifications." });
        }
    }

    /// <summary>
    /// Deletes a specific notification.
    /// </summary>
    /// <param name="id">The notification ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Notification deleted successfully.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="404">Notification not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("DeleteNotification called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        try
        {
            var notification = await _notificationRepository.GetByIdAsync(id, ct);
            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", id, userId);
                return NotFound(new { error = "Notification not found." });
            }

            // Verify the notification belongs to the authenticated user
            if (notification.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to delete notification {NotificationId} belonging to user {OwnerId}",
                    userId, id, notification.UserId);
                return NotFound(new { error = "Notification not found." });
            }

            var deleted = await _notificationRepository.DeleteAsync(id, ct);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete notification {NotificationId} for user {UserId}", id, userId);
                return NotFound(new { error = "Notification not found." });
            }

            _logger.LogInformation("Notification {NotificationId} deleted for user {UserId}", id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", id, userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the notification." });
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

/// <summary>
/// DTO for notification responses.
/// </summary>
public class NotificationDto
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the related resource ID (Location, Collection, etc.).
    /// </summary>
    public Guid? RelatedResourceId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the notification has been read.
    /// </summary>
    public required bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the notification was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
}
