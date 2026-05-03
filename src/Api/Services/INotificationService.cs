namespace LocationManagement.Api.Services;

/// <summary>
/// Defines operations for creating in-app notifications for workflow events.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a notification for the location creator when a non-creator submits a pending edit.
    /// </summary>
    /// <param name="creatorUserId">The ID of the location creator (notification recipient).</param>
    /// <param name="locationId">The ID of the location being edited.</param>
    /// <param name="locationName">The name of the location.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyPendingEditSubmittedAsync(Guid creatorUserId, Guid locationId, string locationName, CancellationToken ct);

    /// <summary>
    /// Creates a notification for the edit submitter when their pending edit is approved.
    /// </summary>
    /// <param name="submitterUserId">The ID of the user who submitted the edit (notification recipient).</param>
    /// <param name="locationId">The ID of the location.</param>
    /// <param name="locationName">The name of the location.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyEditApprovedAsync(Guid submitterUserId, Guid locationId, string locationName, CancellationToken ct);

    /// <summary>
    /// Creates a notification for the edit submitter when their pending edit is rejected.
    /// </summary>
    /// <param name="submitterUserId">The ID of the user who submitted the edit (notification recipient).</param>
    /// <param name="locationId">The ID of the location.</param>
    /// <param name="locationName">The name of the location.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyEditRejectedAsync(Guid submitterUserId, Guid locationId, string locationName, CancellationToken ct);

    /// <summary>
    /// Creates a notification for a user when their membership request to a collection is approved.
    /// </summary>
    /// <param name="requesterUserId">The ID of the user who requested membership (notification recipient).</param>
    /// <param name="collectionId">The ID of the collection.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyMembershipApprovedAsync(Guid requesterUserId, Guid collectionId, string collectionName, CancellationToken ct);

    /// <summary>
    /// Creates a notification for a user when their membership request to a collection is rejected.
    /// </summary>
    /// <param name="requesterUserId">The ID of the user who requested membership (notification recipient).</param>
    /// <param name="collectionId">The ID of the collection.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyMembershipRejectedAsync(Guid requesterUserId, Guid collectionId, string collectionName, CancellationToken ct);
}
