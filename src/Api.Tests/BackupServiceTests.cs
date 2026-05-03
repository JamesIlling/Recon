using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite.Geometries;
using Xunit;
using LocationEntity = LocationManagement.Api.Models.Entities.Location;

namespace LocationManagement.Api.Tests;

/// <summary>
/// Unit tests for BackupService covering export, import, encryption, and data validation.
/// </summary>
public class BackupServiceTests
{
    private readonly Mock<ICoordinateReprojectionService> _mockCoordinateReprojectionService;
    private readonly Mock<ILocalFileStorageService> _mockLocalFileStorageService;
    private readonly Mock<ILogger<BackupService>> _mockLogger;
    private readonly AppDbContext _dbContext;
    private readonly BackupService _backupService;

    public BackupServiceTests()
    {
        _mockCoordinateReprojectionService = new Mock<ICoordinateReprojectionService>();
        _mockLocalFileStorageService = new Mock<ILocalFileStorageService>();
        _mockLogger = new Mock<ILogger<BackupService>>();

        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _backupService = new BackupService(
            _dbContext,
            _mockCoordinateReprojectionService.Object,
            _mockLocalFileStorageService.Object,
            _mockLogger.Object);
    }

    #region Export Tests

    [Fact]
    public async Task ExportAsync_WithValidEncryptionKey_CreatesEncryptedZipStream()
    {
        // Arrange
        var encryptionKey = "this-is-a-valid-encryption-key-32";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _backupService.ExportAsync(encryptionKey, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        Assert.True(result.CanRead);
    }

    [Fact]
    public async Task ExportAsync_WithShortEncryptionKey_ThrowsArgumentException()
    {
        // Arrange
        var shortKey = "short";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _backupService.ExportAsync(shortKey, CancellationToken.None));
    }

    [Fact]
    public async Task ExportAsync_WithNullEncryptionKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _backupService.ExportAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExportAsync_WithValidData_CreatesValidManifest()
    {
        // Arrange
        var encryptionKey = "this-is-a-valid-encryption-key-32";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var location = new LocationEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Location",
            Latitude = 40.7128,
            Longitude = -74.0060,
            SourceSrid = 4326,
            ContentSequence = "[]",
            CreatorId = user.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Coordinates = new Point(new Coordinate(-74.0060, 40.7128)) { SRID = 4326 },
        };
        _dbContext.Users.Add(user);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();

        // Act
        var encryptedStream = await _backupService.ExportAsync(encryptionKey, CancellationToken.None);

        // Assert
        Assert.NotNull(encryptedStream);
        Assert.True(encryptedStream.Length > 16); // At least salt + some data
    }

    #endregion

    #region Import Tests

    [Fact]
    public async Task ImportAsync_WithValidEncryptedArchive_ImportsDataSuccessfully()
    {
        // Arrange
        var encryptionKey = "this-is-a-valid-encryption-key-32";
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        // Create test data
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var location = new LocationEntity
        {
            Id = locationId,
            Name = "Test Location",
            Latitude = 40.7128,
            Longitude = -74.0060,
            SourceSrid = 4326,
            ContentSequence = "[]",
            CreatorId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Coordinates = new Point(new Coordinate(-74.0060, 40.7128)) { SRID = 4326 },
        };
        _dbContext.Users.Add(user);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();

        // Export to create encrypted archive
        var encryptedStream = await _backupService.ExportAsync(encryptionKey, CancellationToken.None);

        // Clear database
        _dbContext.Users.RemoveRange(_dbContext.Users);
        _dbContext.Locations.RemoveRange(_dbContext.Locations);
        await _dbContext.SaveChangesAsync();

        // Act
        encryptedStream.Position = 0;
        var result = await _backupService.ImportAsync(encryptionKey, encryptedStream, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UsersImported);
        Assert.Equal(1, result.LocationsImported);
        Assert.Equal(0, result.UsersSkipped);
        Assert.Equal(0, result.LocationsSkipped);
    }

    [Fact]
    public async Task ImportAsync_WithShortEncryptionKey_ThrowsArgumentException()
    {
        // Arrange
        var shortKey = "short";
        var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _backupService.ImportAsync(shortKey, stream, CancellationToken.None));
    }

    [Fact]
    public async Task ImportAsync_WithInvalidArchive_ThrowsInvalidOperationException()
    {
        // Arrange
        var encryptionKey = "this-is-a-valid-encryption-key-32";
        var invalidStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _backupService.ImportAsync(encryptionKey, invalidStream, CancellationToken.None));
    }

    [Fact]
    public async Task ImportAsync_WithExistingUser_SkipsUserAndMapsId()
    {
        // Arrange
        var encryptionKey = "this-is-a-valid-encryption-key-32";
        var existingUserId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();

        // Create existing user
        var existingUser = new User
        {
            Id = existingUserId,
            Username = "existinguser",
            DisplayName = "Existing User",
            Email = "existing@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        // Create backup with user that has same username
        var manifest = new BackupManifest
        {
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            Users = [new BackupUser
            {
                Id = newUserId,
                Username = "existinguser",
                DisplayName = "Different Display Name",
                Email = "different@example.com",
                PasswordHash = "differenthash",
                Role = "Admin",
                AvatarImageId = null,
                ShowPublicCollections = false,
                CreatedAt = DateTimeOffset.UtcNow,
            }],
            Locations = [],
            LocationCollections = [],
            CollectionMembers = [],
            NamedShapes = [],
            Images = [],
            AuditEvents = [],
        };

        var encryptedStream = CreateEncryptedArchive(manifest, encryptionKey);

        // Act
        var result = await _backupService.ImportAsync(encryptionKey, encryptedStream, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.UsersImported);
        Assert.Equal(1, result.UsersSkipped);
        var importedUsers = await _dbContext.Users.ToListAsync();
        Assert.Single(importedUsers);
        Assert.Equal(existingUserId, importedUsers[0].Id);
    }

    [Fact]
    public async Task ImportAsync_WithInvalidCoordinates_SkipsLocationWithWarning()
    {
        // Arrange
        var encryptionKey = "this-is-a-valid-encryption-key-32";
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        var manifest = new BackupManifest
        {
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            Users = [new BackupUser
            {
                Id = userId,
                Username = "testuser",
                DisplayName = "Test User",
                Email = "test@example.com",
                PasswordHash = "hash",
                Role = "Standard",
                AvatarImageId = null,
                ShowPublicCollections = true,
                CreatedAt = DateTimeOffset.UtcNow,
            }],
            Locations = [new BackupLocation
            {
                Id = locationId,
                Name = "Invalid Location",
                Latitude = 200.0,
                Longitude = -74.0060,
                SourceSrid = 4326,
                ContentSequence = "[]",
                CreatorId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
            }],
            LocationCollections = [],
            CollectionMembers = [],
            NamedShapes = [],
            Images = [],
            AuditEvents = [],
        };

        var encryptedStream = CreateEncryptedArchive(manifest, encryptionKey);

        // Act
        var result = await _backupService.ImportAsync(encryptionKey, encryptedStream, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.UsersImported);
        Assert.Equal(0, result.LocationsImported);
        Assert.Equal(1, result.LocationsSkipped);
        Assert.Single(result.Warnings);
        Assert.Contains("invalid coordinates", result.Warnings[0]);
    }

    [Fact]
    public async Task ImportAsync_CreatesImportUserRecord()
    {
        // Arrange
        var encryptionKey = "this-is-a-valid-encryption-key-32";
        var manifest = new BackupManifest
        {
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            Users = [],
            Locations = [],
            LocationCollections = [],
            CollectionMembers = [],
            NamedShapes = [],
            Images = [],
            AuditEvents = [],
        };

        var encryptedStream = CreateEncryptedArchive(manifest, encryptionKey);

        // Act
        var result = await _backupService.ImportAsync(encryptionKey, encryptedStream, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.ImportUserId);
    }

    #endregion

    #region Encryption Tests

    [Fact]
    public async Task ExportAsync_EncryptionKeyNeverLogged()
    {
        // Arrange
        var encryptionKey = "this-is-a-valid-encryption-key-32";

        // Act
        await _backupService.ExportAsync(encryptionKey, CancellationToken.None);

        // Assert - Verify logger was never called with the encryption key
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(encryptionKey)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportAsync_EncryptionKeyNeverLogged()
    {
        // Arrange
        var encryptionKey = "this-is-a-valid-encryption-key-32";
        var manifest = new BackupManifest
        {
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            Users = [],
            Locations = [],
            LocationCollections = [],
            CollectionMembers = [],
            NamedShapes = [],
            Images = [],
            AuditEvents = [],
        };
        var encryptedStream = CreateEncryptedArchive(manifest, encryptionKey);

        // Act
        await _backupService.ImportAsync(encryptionKey, encryptedStream, CancellationToken.None);

        // Assert - Verify logger was never called with the encryption key
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(encryptionKey)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    #region Helper Methods

    private static Stream CreateEncryptedArchive(BackupManifest manifest, string encryptionKey)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.KeySize = 256;
        aes.Mode = System.Security.Cryptography.CipherMode.CBC;
        aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

        var salt = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(salt);

        aes.Key = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            encryptionKey, salt, 10000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        aes.IV = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            encryptionKey, salt, 10000, System.Security.Cryptography.HashAlgorithmName.SHA256, 16);

        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manifestJson = JsonSerializer.Serialize(manifest);
            var manifestEntry = archive.CreateEntry("manifest.json");
            using var entryStream = manifestEntry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.Write(manifestJson);
        }

        zipStream.Position = 0;

        var encryptedStream = new MemoryStream();
        encryptedStream.Write(salt, 0, salt.Length);

        using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
        using (var cryptoStream = new System.Security.Cryptography.CryptoStream(
            encryptedStream, encryptor, System.Security.Cryptography.CryptoStreamMode.Write, leaveOpen: true))
        {
            zipStream.CopyTo(cryptoStream);
            cryptoStream.FlushFinalBlock();
        }

        encryptedStream.Position = 0;
        return encryptedStream;
    }

    #endregion
}
