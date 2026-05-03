using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Repositories;
using LocationManagement.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocationManagement.Api.Tests;

/// <summary>
/// Integration tests verifying that notifications are created correctly for each workflow event.
/// Tests the integration between LocationService, CollectionService, and NotificationService.
/// </summary>
public class NotificationIntegrationTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ICoordinateReprojectionService> _mockCoordinateReprojectionService;
    private readonly Mock<IImageProcessingService> _mockImageProcessingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<LocationService>> _mockLocationLogger;
    private readonly Mock<ILogger<CollectionService>> _mockCollectionLogger;
    private readonly Mock<ILogger<NotificationService>> _mockNotificationLogger;
    private readonly ILocationRepository _locationRepository;
    private readonly IPendingEditRepository _pendingEditRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IUserRepository _userRepository;
    private readonly LocationService _locationService;
    private readonly CollectionService _collectionService;
    private readonly NotificationService _notificationService;

    public NotificationIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);

        _mockCoordinateReprojectionService = new Mock<ICoordinateReprojectionService>();
        _mockImageProcessingService = new Mock<IImageProcessingService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLocationLogger = new Mock<ILogger<LocationService>>();
        _mockCollectionLogger = new Mock<ILogger<CollectionService>>();
        _mockNotificationLogger = new Mock<ILogger<NotificationService>>();

        _locationRepository = new LocationRepository(_dbContext);
        _pendingEditRepository = new PendingEditRepository(_dbContext);
        _notificationRepository = new NotificationRepository(_dbContext);
        _collectionRepository = new CollectionRepository(_dbContext);
        _userRepository = new UserRepository(_dbContext);

        _notificationService = new NotificationService(
            _notificationRepository,
            _mockNotificationLogger.Object);

        _locationService = new LocationService(
            _dbContext,
            _locationRepository,
            _pendingEditRepository,
            _mockCoordinateReprojectionService.Object,
            _mockImageProcessingService.Object,
            _mockAuditService.Object,
            _notificationService,
            _mockLocationLogger.Object);

        _collectionService = new CollectionService(
            _collectionRepository,
            _dbContext,
            _mockAuditService.Object,
            _notificationService,
            _mockImageProcessingService.Object,
            _mockCacheService.Object,
            _mockCollectionLogger.Object);

        // Setup coordinate reprojection mock to return same coordinates
        _mockCoordinateReprojectionService
            .Setup(x => x.IsSridSupported(It.IsAny<int>()))
            .Returns(true);

        _mockCoordinateReprojectionService
            .Setup(x => x.ReprojectToWgs84(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>()))
            .Returns<double, double, int>((lat, lon, srid) => (lat, lon));
    }

    #region PendingEdit Submitted Tests

    [Fact]
    public async Task SubmitPendingEditAsync_CreatesNotificationForCreator()
    {
        // Arrange
        var creator = CreateTestUser("creator", "Creator User");
        var submitter = CreateTestUser("submitter", "Submitter User");
        await _dbContext.SaveChangesAsync();

        var location = CreateTestLocation(creator.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var contentJson = """[{"type":"Paragraph","text":"Updated content"}]""";

        // Act
        await _locationService.SubmitPendingEditAsync(
            location.Id,
            "Updated Location",
            40.7128,
            -74.0060,
            4326,
            contentJson,
            submitter.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert
        var notifications = await _dbContext.Notifications
            .Where(n => n.UserId == creator.Id && n.Type == NotificationType.PendingEditSubmitted)
            .ToListAsync();

        Assert.Single(notifications);
        var notification = notifications[0];
        Assert.Equal(creator.Id, notification.UserId);
        Assert.Equal(location.Id, notification.RelatedResourceId);
        Assert.Equal(NotificationType.PendingEditSubmitted, notification.Type);
        Assert.Contains(location.Name, notification.Message);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task SubmitPendingEditAsync_NotificationPersistedToDatabase()
    {
        // Arrange
        var creator = CreateTestUser("creator", "Creator User");
        var submitter = CreateTestUser("submitter", "Submitter User");
        await _dbContext.SaveChangesAsync();

        var location = CreateTestLocation(creator.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var contentJson = """[{"type":"Paragraph","text":"Updated content"}]""";

        // Act
        await _locationService.SubmitPendingEditAsync(
            location.Id,
            "Updated Location",
            40.7128,
            -74.0060,
            4326,
            contentJson,
            submitter.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert - verify notification is persisted
        var notificationCount = await _dbContext.Notifications.CountAsync();
        Assert.Equal(1, notificationCount);

        var notification = await _dbContext.Notifications.FirstAsync();
        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.NotNull(notification.Message);
    }

    #endregion

    #region PendingEdit Approved Tests

    [Fact]
    public async Task ApprovePendingEditAsync_CreatesNotificationForSubmitter()
    {
        // Arrange
        var creator = CreateTestUser("creator", "Creator User");
        var submitter = CreateTestUser("submitter", "Submitter User");
        await _dbContext.SaveChangesAsync();

        var location = CreateTestLocation(creator.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var contentJson = """[{"type":"Paragraph","text":"Updated content"}]""";
        var pendingEdit = await _locationService.SubmitPendingEditAsync(
            location.Id,
            "Updated Location",
            40.7128,
            -74.0060,
            4326,
            contentJson,
            submitter.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Clear previous notifications
        var previousNotifications = await _dbContext.Notifications.ToListAsync();
        _dbContext.Notifications.RemoveRange(previousNotifications);
        await _dbContext.SaveChangesAsync();

        // Act
        await _locationService.ApprovePendingEditAsync(
            location.Id,
            pendingEdit.Id,
            creator.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert
        var notifications = await _dbContext.Notifications
            .Where(n => n.UserId == submitter.Id && n.Type == NotificationType.PendingEditApproved)
            .ToListAsync();

        Assert.Single(notifications);
        var notification = notifications[0];
        Assert.Equal(submitter.Id, notification.UserId);
        Assert.Equal(location.Id, notification.RelatedResourceId);
        Assert.Equal(NotificationType.PendingEditApproved, notification.Type);
        Assert.Contains(location.Name, notification.Message);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task ApprovePendingEditAsync_NotificationPersistedToDatabase()
    {
        // Arrange
        var creator = CreateTestUser("creator", "Creator User");
        var submitter = CreateTestUser("submitter", "Submitter User");
        await _dbContext.SaveChangesAsync();

        var location = CreateTestLocation(creator.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var contentJson = """[{"type":"Paragraph","text":"Updated content"}]""";
        var pendingEdit = await _locationService.SubmitPendingEditAsync(
            location.Id,
            "Updated Location",
            40.7128,
            -74.0060,
            4326,
            contentJson,
            submitter.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Clear previous notifications
        var previousNotifications = await _dbContext.Notifications.ToListAsync();
        _dbContext.Notifications.RemoveRange(previousNotifications);
        await _dbContext.SaveChangesAsync();

        // Act
        await _locationService.ApprovePendingEditAsync(
            location.Id,
            pendingEdit.Id,
            creator.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert - verify notification is persisted
        var notificationCount = await _dbContext.Notifications.CountAsync();
        Assert.Equal(1, notificationCount);

        var notification = await _dbContext.Notifications.FirstAsync();
        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.NotNull(notification.Message);
    }

    [Fact]
    public async Task ApprovePendingEditAsync_CorrectUserReceivesNotification()
    {
        // Arrange
        var creator = CreateTestUser("creator", "Creator User");
        var submitter = CreateTestUser("submitter", "Submitter User");
        var otherUser = CreateTestUser("other", "Other User");
        await _dbContext.SaveChangesAsync();

        var location = CreateTestLocation(creator.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var contentJson = """[{"type":"Paragraph","text":"Updated content"}]""";
        var pendingEdit = await _locationService.SubmitPendingEditAsync(
            location.Id,
            "Updated Location",
            40.7128,
            -74.0060,
            4326,
            contentJson,
            submitter.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Clear previous notifications
        var previousNotifications = await _dbContext.Notifications.ToListAsync();
        _dbContext.Notifications.RemoveRange(previousNotifications);
        await _dbContext.SaveChangesAsync();

        // Act
        await _locationService.ApprovePendingEditAsync(
            location.Id,
            pendingEdit.Id,
            creator.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert - only submitter receives notification, not creator or other users
        var submitterNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == submitter.Id)
            .ToListAsync();
        var creatorNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == creator.Id)
            .ToListAsync();
        var otherNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == otherUser.Id)
            .ToListAsync();

        Assert.Single(submitterNotifications);
        Assert.Empty(creatorNotifications);
        Assert.Empty(otherNotifications);
    }

    #endregion

    #region PendingEdit Rejected Tests

    [Fact]
    public async Task RejectPendingEditAsync_CreatesNotificationForSubmitter()
    {
        // Arrange
        var creator = CreateTestUser("creator", "Creator User");
        var submitter = CreateTestUser("submitter", "Submitter User");
        await _dbContext.SaveChangesAsync();

        var location = CreateTestLocation(creator.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var contentJson = """[{"type":"Paragraph","text":"Updated content"}]""";
        var pendingEdit = await _locationService.SubmitPendingEditAsync(
            location.Id,
            "Updated Location",
            40.7128,
            -74.0060,
            4326,
            contentJson,
            submitter.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Clear previous notifications
        var previousNotifications = await _dbContext.Notifications.ToListAsync();
        _dbContext.Notifications.RemoveRange(previousNotifications);
        await _dbContext.SaveChangesAsync();

        // Act
        await _locationService.RejectPendingEditAsync(
            location.Id,
            pendingEdit.Id,
            creator.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert
        var notifications = await _dbContext.Notifications
            .Where(n => n.UserId == submitter.Id && n.Type == NotificationType.PendingEditRejected)
            .ToListAsync();

        Assert.Single(notifications);
        var notification = notifications[0];
        Assert.Equal(submitter.Id, notification.UserId);
        Assert.Equal(location.Id, notification.RelatedResourceId);
        Assert.Equal(NotificationType.PendingEditRejected, notification.Type);
        Assert.Contains(location.Name, notification.Message);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task RejectPendingEditAsync_NotificationPersistedToDatabase()
    {
        // Arrange
        var creator = CreateTestUser("creator", "Creator User");
        var submitter = CreateTestUser("submitter", "Submitter User");
        await _dbContext.SaveChangesAsync();

        var location = CreateTestLocation(creator.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var contentJson = """[{"type":"Paragraph","text":"Updated content"}]""";
        var pendingEdit = await _locationService.SubmitPendingEditAsync(
            location.Id,
            "Updated Location",
            40.7128,
            -74.0060,
            4326,
            contentJson,
            submitter.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Clear previous notifications
        var previousNotifications = await _dbContext.Notifications.ToListAsync();
        _dbContext.Notifications.RemoveRange(previousNotifications);
        await _dbContext.SaveChangesAsync();

        // Act
        await _locationService.RejectPendingEditAsync(
            location.Id,
            pendingEdit.Id,
            creator.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert - verify notification is persisted
        var notificationCount = await _dbContext.Notifications.CountAsync();
        Assert.Equal(1, notificationCount);

        var notification = await _dbContext.Notifications.FirstAsync();
        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.NotNull(notification.Message);
    }

    [Fact]
    public async Task RejectPendingEditAsync_CorrectUserReceivesNotification()
    {
        // Arrange
        var creator = CreateTestUser("creator", "Creator User");
        var submitter = CreateTestUser("submitter", "Submitter User");
        var otherUser = CreateTestUser("other", "Other User");
        await _dbContext.SaveChangesAsync();

        var location = CreateTestLocation(creator.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var contentJson = """[{"type":"Paragraph","text":"Updated content"}]""";
        var pendingEdit = await _locationService.SubmitPendingEditAsync(
            location.Id,
            "Updated Location",
            40.7128,
            -74.0060,
            4326,
            contentJson,
            submitter.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Clear previous notifications
        var previousNotifications = await _dbContext.Notifications.ToListAsync();
        _dbContext.Notifications.RemoveRange(previousNotifications);
        await _dbContext.SaveChangesAsync();

        // Act
        await _locationService.RejectPendingEditAsync(
            location.Id,
            pendingEdit.Id,
            creator.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert - only submitter receives notification, not creator or other users
        var submitterNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == submitter.Id)
            .ToListAsync();
        var creatorNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == creator.Id)
            .ToListAsync();
        var otherNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == otherUser.Id)
            .ToListAsync();

        Assert.Single(submitterNotifications);
        Assert.Empty(creatorNotifications);
        Assert.Empty(otherNotifications);
    }

    #endregion

    #region Membership Approved Tests

    [Fact]
    public async Task ApproveMembershipAsync_CreatesNotificationForRequester()
    {
        // Arrange
        var owner = CreateTestUser("owner", "Owner User");
        var requester = CreateTestUser("requester", "Requester User");
        await _dbContext.SaveChangesAsync();

        var collection = CreateTestCollection(owner.Id, "Test Collection");
        var location = CreateTestLocation(owner.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var membershipRequest = new PendingMembershipRequest
        {
            Id = Guid.NewGuid(),
            LocationId = location.Id,
            CollectionId = collection.Id,
            RequestedByUserId = requester.Id,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = MembershipRequestStatus.Pending
        };
        _dbContext.PendingMembershipRequests.Add(membershipRequest);
        await _dbContext.SaveChangesAsync();

        // Act
        await _collectionService.ApproveMembershipAsync(
            collection.Id,
            membershipRequest.Id,
            owner.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert
        var notifications = await _dbContext.Notifications
            .Where(n => n.UserId == requester.Id && n.Type == NotificationType.MembershipApproved)
            .ToListAsync();

        Assert.Single(notifications);
        var notification = notifications[0];
        Assert.Equal(requester.Id, notification.UserId);
        Assert.Equal(collection.Id, notification.RelatedResourceId);
        Assert.Equal(NotificationType.MembershipApproved, notification.Type);
        Assert.Contains(collection.Name, notification.Message);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task ApproveMembershipAsync_NotificationPersistedToDatabase()
    {
        // Arrange
        var owner = CreateTestUser("owner", "Owner User");
        var requester = CreateTestUser("requester", "Requester User");
        await _dbContext.SaveChangesAsync();

        var collection = CreateTestCollection(owner.Id, "Test Collection");
        var location = CreateTestLocation(owner.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var membershipRequest = new PendingMembershipRequest
        {
            Id = Guid.NewGuid(),
            LocationId = location.Id,
            CollectionId = collection.Id,
            RequestedByUserId = requester.Id,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = MembershipRequestStatus.Pending
        };
        _dbContext.PendingMembershipRequests.Add(membershipRequest);
        await _dbContext.SaveChangesAsync();

        // Act
        await _collectionService.ApproveMembershipAsync(
            collection.Id,
            membershipRequest.Id,
            owner.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert - verify notification is persisted
        var notificationCount = await _dbContext.Notifications.CountAsync();
        Assert.Equal(1, notificationCount);

        var notification = await _dbContext.Notifications.FirstAsync();
        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.NotNull(notification.Message);
    }

    [Fact]
    public async Task ApproveMembershipAsync_CorrectUserReceivesNotification()
    {
        // Arrange
        var owner = CreateTestUser("owner", "Owner User");
        var requester = CreateTestUser("requester", "Requester User");
        var otherUser = CreateTestUser("other", "Other User");
        await _dbContext.SaveChangesAsync();

        var collection = CreateTestCollection(owner.Id, "Test Collection");
        var location = CreateTestLocation(owner.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var membershipRequest = new PendingMembershipRequest
        {
            Id = Guid.NewGuid(),
            LocationId = location.Id,
            CollectionId = collection.Id,
            RequestedByUserId = requester.Id,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = MembershipRequestStatus.Pending
        };
        _dbContext.PendingMembershipRequests.Add(membershipRequest);
        await _dbContext.SaveChangesAsync();

        // Act
        await _collectionService.ApproveMembershipAsync(
            collection.Id,
            membershipRequest.Id,
            owner.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert - only requester receives notification, not owner or other users
        var requesterNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == requester.Id)
            .ToListAsync();
        var ownerNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == owner.Id)
            .ToListAsync();
        var otherNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == otherUser.Id)
            .ToListAsync();

        Assert.Single(requesterNotifications);
        Assert.Empty(ownerNotifications);
        Assert.Empty(otherNotifications);
    }

    #endregion

    #region Membership Rejected Tests

    [Fact]
    public async Task RejectMembershipAsync_CreatesNotificationForRequester()
    {
        // Arrange
        var owner = CreateTestUser("owner", "Owner User");
        var requester = CreateTestUser("requester", "Requester User");
        await _dbContext.SaveChangesAsync();

        var collection = CreateTestCollection(owner.Id, "Test Collection");
        var location = CreateTestLocation(owner.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var membershipRequest = new PendingMembershipRequest
        {
            Id = Guid.NewGuid(),
            LocationId = location.Id,
            CollectionId = collection.Id,
            RequestedByUserId = requester.Id,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = MembershipRequestStatus.Pending
        };
        _dbContext.PendingMembershipRequests.Add(membershipRequest);
        await _dbContext.SaveChangesAsync();

        // Act
        await _collectionService.RejectMembershipAsync(
            collection.Id,
            membershipRequest.Id,
            owner.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert
        var notifications = await _dbContext.Notifications
            .Where(n => n.UserId == requester.Id && n.Type == NotificationType.MembershipRejected)
            .ToListAsync();

        Assert.Single(notifications);
        var notification = notifications[0];
        Assert.Equal(requester.Id, notification.UserId);
        Assert.Equal(collection.Id, notification.RelatedResourceId);
        Assert.Equal(NotificationType.MembershipRejected, notification.Type);
        Assert.Contains(collection.Name, notification.Message);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task RejectMembershipAsync_NotificationPersistedToDatabase()
    {
        // Arrange
        var owner = CreateTestUser("owner", "Owner User");
        var requester = CreateTestUser("requester", "Requester User");
        await _dbContext.SaveChangesAsync();

        var collection = CreateTestCollection(owner.Id, "Test Collection");
        var location = CreateTestLocation(owner.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var membershipRequest = new PendingMembershipRequest
        {
            Id = Guid.NewGuid(),
            LocationId = location.Id,
            CollectionId = collection.Id,
            RequestedByUserId = requester.Id,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = MembershipRequestStatus.Pending
        };
        _dbContext.PendingMembershipRequests.Add(membershipRequest);
        await _dbContext.SaveChangesAsync();

        // Act
        await _collectionService.RejectMembershipAsync(
            collection.Id,
            membershipRequest.Id,
            owner.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert - verify notification is persisted
        var notificationCount = await _dbContext.Notifications.CountAsync();
        Assert.Equal(1, notificationCount);

        var notification = await _dbContext.Notifications.FirstAsync();
        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.NotNull(notification.Message);
    }

    [Fact]
    public async Task RejectMembershipAsync_CorrectUserReceivesNotification()
    {
        // Arrange
        var owner = CreateTestUser("owner", "Owner User");
        var requester = CreateTestUser("requester", "Requester User");
        var otherUser = CreateTestUser("other", "Other User");
        await _dbContext.SaveChangesAsync();

        var collection = CreateTestCollection(owner.Id, "Test Collection");
        var location = CreateTestLocation(owner.Id, "Test Location");
        await _dbContext.SaveChangesAsync();

        var membershipRequest = new PendingMembershipRequest
        {
            Id = Guid.NewGuid(),
            LocationId = location.Id,
            CollectionId = collection.Id,
            RequestedByUserId = requester.Id,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = MembershipRequestStatus.Pending
        };
        _dbContext.PendingMembershipRequests.Add(membershipRequest);
        await _dbContext.SaveChangesAsync();

        // Act
        await _collectionService.RejectMembershipAsync(
            collection.Id,
            membershipRequest.Id,
            owner.Id,
            "127.0.0.1",
            CancellationToken.None);

        // Assert - only requester receives notification, not owner or other users
        var requesterNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == requester.Id)
            .ToListAsync();
        var ownerNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == owner.Id)
            .ToListAsync();
        var otherNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == otherUser.Id)
            .ToListAsync();

        Assert.Single(requesterNotifications);
        Assert.Empty(ownerNotifications);
        Assert.Empty(otherNotifications);
    }

    #endregion

    #region Helper Methods

    private User CreateTestUser(string username, string displayName)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = displayName,
            Email = $"{username}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Users.Add(user);
        return user;
    }

    private Location CreateTestLocation(Guid creatorId, string name)
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatorId = creatorId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            SourceSrid = 4326,
            Latitude = 40.7128,
            Longitude = -74.0060,
            Coordinates = new NetTopologySuite.Geometries.Point(new NetTopologySuite.Geometries.Coordinate(-74.0060, 40.7128)) { SRID = 4326 },
            ContentSequence = """[{"type":"Paragraph","text":"Test content"}]"""
        };
        _dbContext.Locations.Add(location);
        return location;
    }

    private LocationCollection CreateTestCollection(Guid ownerId, string name)
    {
        var collection = new LocationCollection
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test collection",
            OwnerId = ownerId,
            IsPublic = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.LocationCollections.Add(collection);
        return collection;
    }

    #endregion
}
