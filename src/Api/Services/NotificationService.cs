using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Repositories;

namespace LocationManagement.Api.Services;

/// <summary>
/// Implements notification creation for workflow events.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a notification for the location creator when a pending edit is submitted.
    /// </summary>
    public async Task NotifyPendingEditSubmittedAsync(Guid creatorUserId, Guid locationId, string locationName, CancellationToken ct)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = creatorUserId,
            Type = NotificationType.PendingEditSubmitted,
            Message = $"A pending edit was submitted for location '{locationName}'",
            RelatedResourceId = locationId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.CreateAsync(notification, ct);
        _logger.LogInformation("Notification created: PendingEditSubmitted for location {LocationId} to user {UserId}", locationId, creatorUserId);
    }

    /// <summary>
    /// Creates a notification for the edit submitter when their pending edit is approved.
    /// </summary>
    public async Task NotifyEditApprovedAsync(Guid submitterUserId, Guid locationId, string locationName, CancellationToken ct)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = submitterUserId,
            Type = NotificationType.PendingEditApproved,
            Message = $"Your edit for location '{locationName}' was approved",
            RelatedResourceId = locationId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.CreateAsync(notification, ct);
        _logger.LogInformation("Notification created: EditApproved for location {LocationId} to user {UserId}", locationId, submitterUserId);
    }

    /// <summary>
    /// Creates a notification for the edit submitter when their pending edit is rejected.
    /// </summary>
    public async Task NotifyEditRejectedAsync(Guid submitterUserId, Guid locationId, string locationName, CancellationToken ct)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = submitterUserId,
            Type = NotificationType.PendingEditRejected,
            Message = $"Your edit for location '{locationName}' was rejected",
            RelatedResourceId = locationId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.CreateAsync(notification, ct);
        _logger.LogInformation("Notification created: EditRejected for location {LocationId} to user {UserId}", locationId, submitterUserId);
    }

    /// <summary>
    /// Creates a notification for a user when their membership request is approved.
    /// </summary>
    public async Task NotifyMembershipApprovedAsync(Guid requesterUserId, Guid collectionId, string collectionName, CancellationToken ct)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = requesterUserId,
            Type = NotificationType.MembershipApproved,
            Message = $"Your membership request for collection '{collectionName}' was approved",
            RelatedResourceId = collectionId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.CreateAsync(notification, ct);
        _logger.LogInformation("Notification created: MembershipApproved for collection {CollectionId} to user {UserId}", collectionId, requesterUserId);
    }

    /// <summary>
    /// Creates a notification for a user when their membership request is rejected.
    /// </summary>
    public async Task NotifyMembershipRejectedAsync(Guid requesterUserId, Guid collectionId, string collectionName, CancellationToken ct)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = requesterUserId,
            Type = NotificationType.MembershipRejected,
            Message = $"Your membership request for collection '{collectionName}' was rejected",
            RelatedResourceId = collectionId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.CreateAsync(notification, ct);
        _logger.LogInformation("Notification created: MembershipRejected for collection {CollectionId} to user {UserId}", collectionId, requesterUserId);
    }
}
