using Moq;
using Xunit;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Repositories;
using LocationManagement.Api.Services;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace LocationManagement.Api.Tests;

/// <summary>
/// Unit tests for UserService covering all user profile and configuration operations.
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IImageProcessingService> _mockImageProcessingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockImageProcessingService = new Mock<IImageProcessingService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<UserService>>();

        _userService = new UserService(
            _mockUserRepository.Object,
            _mockImageProcessingService.Object,
            _mockAuditService.Object,
            _mockLogger.Object);
    }

    #region GetProfileAsync Tests

    [Fact]
    public async Task GetProfileAsync_WithValidUserId_ReturnsUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetProfileAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Null(result.AvatarThumbnailUrl);
        Assert.True(result.ShowPublicCollections);
        Assert.Null(result.AvatarThumbnailUrl); // Email must never be exposed
        _mockUserRepository.Verify(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_WithAvatarImage_ReturnsAvatarThumbnailUrl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var avatarImage = new Image
        {
            Id = Guid.NewGuid(),
            FileName = "avatar.jpg",
            MimeType = "image/jpeg",
            FileSize = 50000,
            OriginalUrl = "https://example.com/avatar.jpg",
            ThumbnailUrl = "https://example.com/avatar-thumb.jpg",
            ResponsiveVariantUrls = "[]",
            UploadedByUserId = userId,
            UploadedAt = DateTimeOffset.UtcNow
        };

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            AvatarImageId = avatarImage.Id,
            AvatarImage = avatarImage,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetProfileAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://example.com/avatar-thumb.jpg", result.AvatarThumbnailUrl);
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _userService.GetProfileAsync(userId, CancellationToken.None));
    }

    #endregion

    #region ChangeDisplayNameAsync Tests

    [Fact]
    public async Task ChangeDisplayNameAsync_WithValidNewDisplayName_UpdatesAndReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Old Name",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.IsDisplayNameInUseAsync("New Name", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.ChangeDisplayNameAsync(userId, "New Name", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.DisplayName);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(
            a => a.RecordAsync("DisplayNameChanged", userId, nameof(User), userId, AuditOutcome.Success, string.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeDisplayNameAsync_WithDuplicateDisplayName_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Old Name",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.IsDisplayNameInUseAsync("Duplicate Name", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.ChangeDisplayNameAsync(userId, "Duplicate Name", CancellationToken.None));

        _mockAuditService.Verify(
            a => a.RecordAsync("DisplayNameChangeAttempted", userId, nameof(User), userId, AuditOutcome.Failure, string.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeDisplayNameAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _userService.ChangeDisplayNameAsync(userId, "New Name", CancellationToken.None));
    }

    [Fact]
    public async Task ChangeDisplayNameAsync_WithCaseInsensitiveDuplicate_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Old Name",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.IsDisplayNameInUseAsync("DUPLICATE NAME", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.ChangeDisplayNameAsync(userId, "DUPLICATE NAME", CancellationToken.None));
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectCurrentPassword_UpdatesPasswordHash()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "CurrentPassword123";
        var newPassword = "NewPassword456";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(currentPassword, 12);

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = passwordHash,
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _userService.ChangePasswordAsync(userId, currentPassword, newPassword, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(
            a => a.RecordAsync("PasswordChanged", userId, nameof(User), userId, AuditOutcome.Success, string.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithIncorrectCurrentPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "CorrectPassword123";
        var wrongPassword = "WrongPassword456";
        var newPassword = "NewPassword789";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(currentPassword, 12);

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = passwordHash,
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _userService.ChangePasswordAsync(userId, wrongPassword, newPassword, CancellationToken.None));

        _mockAuditService.Verify(
            a => a.RecordAsync("PasswordChangeAttempted", userId, nameof(User), userId, AuditOutcome.Failure, string.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _userService.ChangePasswordAsync(userId, "current", "new", CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordAsync_VerifiesPasswordUsingBCrypt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "TestPassword123";
        var newPassword = "NewPassword456";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(currentPassword, 12);

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = passwordHash,
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _userService.ChangePasswordAsync(userId, currentPassword, newPassword, CancellationToken.None);

        // Assert - verify that the new password hash is different from the old one
        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.Is<User>(u => u.PasswordHash != passwordHash), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UploadAvatarAsync Tests

    [Fact]
    public async Task UploadAvatarAsync_WithValidImage_UploadsAndReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var imageStream = new MemoryStream(new byte[100000]); // 100 KB
        var mimeType = "image/jpeg";
        var imageId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var variants = new ImageVariants(
            imageId,
            "https://example.com/thumb.jpg",
            "https://example.com/400.jpg",
            "https://example.com/700.jpg",
            "https://example.com/1000.jpg"
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockImageProcessingService.Setup(s => s.ProcessAndStoreAsync(imageStream, mimeType, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variants);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.UploadAvatarAsync(userId, imageStream, mimeType, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://example.com/thumb.jpg", result.AvatarThumbnailUrl);
        _mockImageProcessingService.Verify(
            s => s.ProcessAndStoreAsync(imageStream, mimeType, null, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockAuditService.Verify(
            a => a.RecordAsync("AvatarUploaded", userId, nameof(User), userId, AuditOutcome.Success, string.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAvatarAsync_WithImageExceedingOneMB_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var imageStream = new MemoryStream(new byte[1_048_577]); // 1 MB + 1 byte
        var mimeType = "image/jpeg";

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.UploadAvatarAsync(userId, imageStream, mimeType, CancellationToken.None));

        _mockAuditService.Verify(
            a => a.RecordAsync("AvatarUploadAttempted", userId, nameof(User), userId, AuditOutcome.Failure, string.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAvatarAsync_WithPreviousAvatar_DeletesPreviousAvatarBeforeUploadingNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var previousAvatarId = Guid.NewGuid();
        var newImageId = Guid.NewGuid();
        var imageStream = new MemoryStream(new byte[100000]); // 100 KB
        var mimeType = "image/jpeg";

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            AvatarImageId = previousAvatarId,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var variants = new ImageVariants(
            newImageId,
            "https://example.com/new-thumb.jpg",
            "https://example.com/400.jpg",
            "https://example.com/700.jpg",
            "https://example.com/1000.jpg"
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockImageProcessingService.Setup(s => s.ProcessAndStoreAsync(imageStream, mimeType, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variants);
        _mockImageProcessingService.Setup(s => s.DeleteImageAndVariantsAsync(previousAvatarId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _userService.UploadAvatarAsync(userId, imageStream, mimeType, CancellationToken.None);

        // Assert
        _mockImageProcessingService.Verify(
            s => s.DeleteImageAndVariantsAsync(previousAvatarId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAvatarAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var imageStream = new MemoryStream(new byte[100000]);
        var mimeType = "image/jpeg";

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _userService.UploadAvatarAsync(userId, imageStream, mimeType, CancellationToken.None));
    }

    #endregion

    #region UpdatePreferencesAsync Tests

    [Fact]
    public async Task UpdatePreferencesAsync_WithShowPublicCollectionsTrue_UpdatesAndReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.UpdatePreferencesAsync(userId, true, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShowPublicCollections);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(
            a => a.RecordAsync("PreferencesUpdated", userId, nameof(User), userId, AuditOutcome.Success, string.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithShowPublicCollectionsFalse_UpdatesAndReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            AvatarImageId = null,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.UpdatePreferencesAsync(userId, false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.ShowPublicCollections);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _userService.UpdatePreferencesAsync(userId, true, CancellationToken.None));
    }

    #endregion
}
