using LocationManagement.Api.Models.Enums;

namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents an in-app notification for a user about a workflow event
/// (pending edit submitted/approved/rejected, membership approved/rejected).
/// </summary>
public class Notification
{
    /// <summary>
    /// Gets or sets the unique identifier for the notification.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the user who receives this notification.
    /// Foreign key to User.Id.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the type of notification event.
    /// </summary>
    public required NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets the human-readable message for the notification.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the related resource (Location, PendingEdit, or Collection).
    /// Nullable; some notifications may not have a specific resource.
    /// </summary>
    public Guid? RelatedResourceId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the notification has been read by the user.
    /// Defaults to false.
    /// </summary>
    public required bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the notification was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the navigation property for the user who receives this notification.
    /// </summary>
    public virtual User User { get; set; } = null!;
}
