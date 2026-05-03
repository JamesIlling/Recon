using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Repositories;
using LocationManagement.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocationManagement.Api.Tests;

/// <summary>
/// Unit tests for NotificationService covering all 5 notification methods.
/// </summary>
public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _mockNotificationRepository;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _mockNotificationRepository = new Mock<INotificationRepository>();
        _mockLogger = new Mock<ILogger<NotificationService>>();

        _notificationService = new NotificationService(
            _mockNotificationRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task NotifyPendingEditSubmittedAsync_CreatesNotification()
    {
        var creatorUserId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var locationName = "Test Location";

        _mockNotificationRepository
            .Setup(x => x.CreateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, CancellationToken ct) => n);

        await _notificationService.NotifyPendingEditSubmittedAsync(
            creatorUserId,
            locationId,
            locationName,
            CancellationToken.None);

        _mockNotificationRepository.Verify(
            x => x.CreateAsync(It.Is<Notification>(n =>
                n.UserId == creatorUserId &&
                n.RelatedResourceId == locationId &&
                n.Type == NotificationType.PendingEditSubmitted),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyEditApprovedAsync_CreatesNotification()
    {
        var submitterUserId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var locationName = "Test Location";

        _mockNotificationRepository
            .Setup(x => x.CreateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, CancellationToken ct) => n);

        await _notificationService.NotifyEditApprovedAsync(
            submitterUserId,
            locationId,
            locationName,
            CancellationToken.None);

        _mockNotificationRepository.Verify(
            x => x.CreateAsync(It.Is<Notification>(n =>
                n.UserId == submitterUserId &&
                n.RelatedResourceId == locationId &&
                n.Type == NotificationType.PendingEditApproved),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyEditRejectedAsync_CreatesNotification()
    {
        var submitterUserId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var locationName = "Test Location";

        _mockNotificationRepository
            .Setup(x => x.CreateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, CancellationToken ct) => n);

        await _notificationService.NotifyEditRejectedAsync(
            submitterUserId,
            locationId,
            locationName,
            CancellationToken.None);

        _mockNotificationRepository.Verify(
            x => x.CreateAsync(It.Is<Notification>(n =>
                n.UserId == submitterUserId &&
                n.RelatedResourceId == locationId &&
                n.Type == NotificationType.PendingEditRejected),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyMembershipApprovedAsync_CreatesNotification()
    {
        var requesterUserId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var collectionName = "Test Collection";

        _mockNotificationRepository
            .Setup(x => x.CreateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, CancellationToken ct) => n);

        await _notificationService.NotifyMembershipApprovedAsync(
            requesterUserId,
            collectionId,
            collectionName,
            CancellationToken.None);

        _mockNotificationRepository.Verify(
            x => x.CreateAsync(It.Is<Notification>(n =>
                n.UserId == requesterUserId &&
                n.RelatedResourceId == collectionId &&
                n.Type == NotificationType.MembershipApproved),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyMembershipRejectedAsync_CreatesNotification()
    {
        var requesterUserId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var collectionName = "Test Collection";

        _mockNotificationRepository
            .Setup(x => x.CreateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, CancellationToken ct) => n);

        await _notificationService.NotifyMembershipRejectedAsync(
            requesterUserId,
            collectionId,
            collectionName,
            CancellationToken.None);

        _mockNotificationRepository.Verify(
            x => x.CreateAsync(It.Is<Notification>(n =>
                n.UserId == requesterUserId &&
                n.RelatedResourceId == collectionId &&
                n.Type == NotificationType.MembershipRejected),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
