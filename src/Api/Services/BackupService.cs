using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using LocationEntity = LocationManagement.Api.Models.Entities.Location;

namespace LocationManagement.Api.Services;

/// <summary>
/// Implementation of IBackupService for exporting and importing backup archives with AES-256 encryption.
/// </summary>
public class BackupService : IBackupService
{
    private const int MinimumKeyLength = 32;
    private const int SaltLength = 16;
    private const int Pbkdf2Iterations = 10000;

    private readonly AppDbContext _dbContext;
    private readonly ICoordinateReprojectionService _coordinateReprojectionService;
    private readonly ILocalFileStorageService _fileStorageService;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        AppDbContext dbContext,
        ICoordinateReprojectionService coordinateReprojectionService,
        ILocalFileStorageService fileStorageService,
        ILogger<BackupService> logger)
    {
        _dbContext = dbContext;
        _coordinateReprojectionService = coordinateReprojectionService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Exports all data to an encrypted ZIP archive.
    /// </summary>
    public async Task<Stream> ExportAsync(string encryptionKey, CancellationToken cancellationToken)
    {
        ValidateEncryptionKey(encryptionKey);

        _logger.LogInformation("Starting backup export");

        // Query all exportable data
        var users = await _dbContext.Users.ToListAsync(cancellationToken);
        var locations = await _dbContext.Locations.ToListAsync(cancellationToken);
        var collections = await _dbContext.LocationCollections.ToListAsync(cancellationToken);
        var collectionMembers = await _dbContext.CollectionMembers.ToListAsync(cancellationToken);
        var namedShapes = await _dbContext.NamedShapes.ToListAsync(cancellationToken);
        var images = await _dbContext.Images.ToListAsync(cancellationToken);
        var auditEvents = await _dbContext.AuditEvents.ToListAsync(cancellationToken);

        // Build manifest
        var manifest = new BackupManifest
        {
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            Users = users.Select(u => new BackupUser
            {
                Id = u.Id,
                Username = u.Username,
                DisplayName = u.DisplayName,
                Email = u.Email,
                PasswordHash = u.PasswordHash,
                Role = u.Role.ToString(),
                AvatarImageId = u.AvatarImageId,
                ShowPublicCollections = u.ShowPublicCollections,
                CreatedAt = u.CreatedAt,
            }).ToList(),
            Locations = locations.Select(l => new BackupLocation
            {
                Id = l.Id,
                Name = l.Name,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                SourceSrid = l.SourceSrid,
                ContentSequence = l.ContentSequence,
                CreatorId = l.CreatorId,
                CreatedAt = l.CreatedAt,
            }).ToList(),
            LocationCollections = collections.Select(c => new BackupLocationCollection
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                OwnerId = c.OwnerId,
                ThumbnailImageId = c.ThumbnailImageId,
                BoundingShapeId = c.BoundingShapeId,
                IsPublic = c.IsPublic,
                CreatedAt = c.CreatedAt,
            }).ToList(),
            CollectionMembers = collectionMembers.Select(cm => new BackupCollectionMember
            {
                LocationId = cm.LocationId,
                CollectionId = cm.CollectionId,
            }).ToList(),
            NamedShapes = namedShapes.Select(ns => new BackupNamedShape
            {
                Id = ns.Id,
                Name = ns.Name,
                Geometry = ns.Geometry.AsText(),
                CreatedByUserId = ns.CreatedByUserId,
                CreatedAt = ns.CreatedAt,
            }).ToList(),
            Images = images.Select(i => new BackupImage
            {
                Id = i.Id,
                FileName = i.FileName,
                MimeType = i.MimeType,
                AltText = i.AltText,
                FileSize = i.FileSize,
                UploadedByUserId = i.UploadedByUserId,
                UploadedAt = i.UploadedAt,
            }).ToList(),
            AuditEvents = auditEvents.Select(ae => new BackupAuditEvent
            {
                EventType = ae.EventType,
                ActingUserId = ae.ActingUserId,
                ResourceType = ae.ResourceType,
                ResourceId = ae.ResourceId,
                Outcome = ae.Outcome.ToString(),
                CreatedAt = ae.CreatedAt,
            }).ToList(),
        };

        // Create ZIP with manifest and image files
        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Add manifest
            var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            var manifestEntry = archive.CreateEntry("manifest.json");
            using (var entryStream = manifestEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                await writer.WriteAsync(manifestJson);
            }

            // Add image files
            foreach (var image in images)
            {
                try
                {
                    var imageData = await _fileStorageService.RetrieveAsync(image.FileName, cancellationToken);
                    if (imageData != null)
                    {
                        var imageEntry = archive.CreateEntry($"images/{image.Id}");
                        using (var entryStream = imageEntry.Open())
                        {
                            await imageData.CopyToAsync(entryStream, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to include image {ImageId} in backup", image.Id);
                }
            }
        }

        zipStream.Position = 0;

        // Encrypt the ZIP
        var encryptedStream = EncryptStream(zipStream, encryptionKey);

        _logger.LogInformation("Backup export completed successfully");
        return encryptedStream;
    }

    /// <summary>
    /// Imports data from an encrypted ZIP archive.
    /// </summary>
    public async Task<ImportResult> ImportAsync(string encryptionKey, Stream encryptedStream, CancellationToken cancellationToken)
    {
        ValidateEncryptionKey(encryptionKey);

        _logger.LogInformation("Starting backup import");

        var result = new ImportResult
        {
            ImportUserId = Guid.NewGuid(),
            UsersImported = 0,
            UsersSkipped = 0,
            LocationsImported = 0,
            LocationsSkipped = 0,
            CollectionsImported = 0,
            CollectionsSkipped = 0,
            MembersImported = 0,
            MembersSkipped = 0,
            NamedShapesImported = 0,
            NamedShapesSkipped = 0,
            ImagesImported = 0,
            ImagesSkipped = 0,
            Warnings = [],
        };

        try
        {
            // Decrypt the stream — wrap low-level crypto/stream errors as InvalidOperationException
            // so callers receive a consistent contract (HTTP 422 at the controller layer).
            Stream decryptedStream;
            try
            {
                decryptedStream = DecryptStream(encryptedStream, encryptionKey);
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                throw new InvalidOperationException("The backup archive could not be decrypted. It may be corrupted or the key is incorrect.", ex);
            }

            decryptedStream.Position = 0;

            // Extract and parse manifest
            BackupManifest manifest;
            using (var archive = new ZipArchive(decryptedStream, ZipArchiveMode.Read))
            {
                var manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry == null)
                {
                    throw new InvalidOperationException("Backup archive does not contain manifest.json");
                }

                using (var entryStream = manifestEntry.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    var json = await reader.ReadToEndAsync(cancellationToken);
                    manifest = JsonSerializer.Deserialize<BackupManifest>(json)
                        ?? throw new InvalidOperationException("Failed to deserialize manifest");
                }
            }

            // Create ID mapping for imported users
            var userIdMap = new Dictionary<Guid, Guid>();

            // Import users
            foreach (var backupUser in manifest.Users)
            {
                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == backupUser.Username, cancellationToken);

                if (existingUser != null)
                {
                    userIdMap[backupUser.Id] = existingUser.Id;
                    result.UsersSkipped++;
                    result.Warnings.Add($"User '{backupUser.Username}' already exists; skipped");
                    continue;
                }

                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = backupUser.Username,
                    DisplayName = backupUser.DisplayName,
                    Email = backupUser.Email,
                    PasswordHash = backupUser.PasswordHash,
                    Role = Enum.Parse<UserRole>(backupUser.Role),
                    AvatarImageId = backupUser.AvatarImageId,
                    ShowPublicCollections = backupUser.ShowPublicCollections,
                    CreatedAt = backupUser.CreatedAt,
                    UpdatedAt = backupUser.CreatedAt,
                };

                userIdMap[backupUser.Id] = newUser.Id;
                _dbContext.Users.Add(newUser);
                result.UsersImported++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Import locations
            var locationIdMap = new Dictionary<Guid, Guid>();
            foreach (var backupLocation in manifest.Locations)
            {
                if (!userIdMap.TryGetValue(backupLocation.CreatorId, out var newCreatorId))
                {
                    result.LocationsSkipped++;
                    result.Warnings.Add($"Location '{backupLocation.Name}' creator not found; skipped");
                    continue;
                }

                // Validate coordinates
                if (backupLocation.Latitude < -90 || backupLocation.Latitude > 90 ||
                    backupLocation.Longitude < -180 || backupLocation.Longitude > 180)
                {
                    result.LocationsSkipped++;
                    result.Warnings.Add($"Location '{backupLocation.Name}' has invalid coordinates; skipped");
                    continue;
                }

                var newLocationId = Guid.NewGuid();
                var newLocation = new LocationEntity
                {
                    Id = newLocationId,
                    Name = backupLocation.Name,
                    Latitude = backupLocation.Latitude,
                    Longitude = backupLocation.Longitude,
                    SourceSrid = backupLocation.SourceSrid,
                    ContentSequence = backupLocation.ContentSequence,
                    CreatorId = newCreatorId,
                    CreatedAt = backupLocation.CreatedAt,
                    UpdatedAt = backupLocation.CreatedAt,
                    Coordinates = new Point(new Coordinate(backupLocation.Longitude, backupLocation.Latitude))
                    {
                        SRID = 4326,
                    },
                };

                locationIdMap[backupLocation.Id] = newLocationId;
                _dbContext.Locations.Add(newLocation);
                result.LocationsImported++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Import collections
            var collectionIdMap = new Dictionary<Guid, Guid>();
            foreach (var backupCollection in manifest.LocationCollections)
            {
                if (!userIdMap.TryGetValue(backupCollection.OwnerId, out var newOwnerId))
                {
                    result.Warnings.Add($"Collection '{backupCollection.Name}' owner not found; skipped");
                    continue;
                }

                var newCollectionId = Guid.NewGuid();
                var newCollection = new LocationCollection
                {
                    Id = newCollectionId,
                    Name = backupCollection.Name,
                    Description = backupCollection.Description,
                    OwnerId = newOwnerId,
                    ThumbnailImageId = backupCollection.ThumbnailImageId,
                    BoundingShapeId = backupCollection.BoundingShapeId,
                    IsPublic = backupCollection.IsPublic,
                    CreatedAt = backupCollection.CreatedAt,
                    UpdatedAt = backupCollection.CreatedAt,
                };

                collectionIdMap[backupCollection.Id] = newCollectionId;
                _dbContext.LocationCollections.Add(newCollection);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Import collection members
            foreach (var backupMember in manifest.CollectionMembers)
            {
                if (!locationIdMap.TryGetValue(backupMember.LocationId, out var newLocationId) ||
                    !collectionIdMap.TryGetValue(backupMember.CollectionId, out var newCollectionId))
                {
                    continue;
                }

                var newMember = new CollectionMember
                {
                    LocationId = newLocationId,
                    CollectionId = newCollectionId,
                    AddedAt = DateTimeOffset.UtcNow,
                };

                _dbContext.CollectionMembers.Add(newMember);
                result.MembersImported++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Backup import completed: {UsersImported} users, {LocationsImported} locations",
                result.UsersImported,
                result.LocationsImported);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup import failed");
            throw;
        }

        return result;
    }

    private static void ValidateEncryptionKey(string encryptionKey)
    {
        if (string.IsNullOrEmpty(encryptionKey))
        {
            throw new ArgumentException("Encryption key cannot be null or empty", nameof(encryptionKey));
        }

        if (encryptionKey.Length < MinimumKeyLength)
        {
            throw new ArgumentException(
                $"Encryption key must be at least {MinimumKeyLength} characters long",
                nameof(encryptionKey));
        }
    }

    private static Stream EncryptStream(Stream plainStream, string encryptionKey)
    {
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Generate salt
            var salt = new byte[SaltLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Derive key and IV from password using PBKDF2
            var keyBytes = Rfc2898DeriveBytes.Pbkdf2(
                encryptionKey, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 32);
            var ivBytes = Rfc2898DeriveBytes.Pbkdf2(
                encryptionKey, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 16);

            aes.Key = keyBytes;
            aes.IV = ivBytes;

            var encryptedStream = new MemoryStream();

            // Write salt to beginning of stream
            encryptedStream.Write(salt, 0, salt.Length);

            // Encrypt the data
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var cryptoStream = new CryptoStream(
                encryptedStream, encryptor, CryptoStreamMode.Write, leaveOpen: true))
            {
                plainStream.CopyTo(cryptoStream);
                cryptoStream.FlushFinalBlock();
            }

            encryptedStream.Position = 0;
            return encryptedStream;
        }
    }

    private static Stream DecryptStream(Stream encryptedStream, string encryptionKey)
    {
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Read salt from beginning of stream
            var salt = new byte[SaltLength];
            encryptedStream.ReadExactly(salt, 0, SaltLength);

            // Derive key and IV from password using PBKDF2
            var keyBytes = Rfc2898DeriveBytes.Pbkdf2(
                encryptionKey, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 32);
            var ivBytes = Rfc2898DeriveBytes.Pbkdf2(
                encryptionKey, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 16);

            aes.Key = keyBytes;
            aes.IV = ivBytes;

            var decryptedStream = new MemoryStream();

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var cryptoStream = new CryptoStream(
                encryptedStream, decryptor, CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(decryptedStream);
            }

            decryptedStream.Position = 0;
            return decryptedStream;
        }
    }
}
