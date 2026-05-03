using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LocationManagement.Api.Services;

/// <summary>
/// Service implementation for admin user management operations.
/// </summary>
public sealed class UserAdminService : IUserAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserAdminService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAdminService"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="locationRepository">The location repository.</param>
    /// <param name="collectionRepository">The collection repository.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="logger">The logger.</param>
    public UserAdminService(
        IUserRepository userRepository,
        ILocationRepository locationRepository,
        ICollectionRepository collectionRepository,
        IAuditService auditService,
        ILogger<UserAdminService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all users with pagination, ordered by creation date descending.
    /// </summary>
    /// <param name="page">The page number (1-indexed).</param>
    /// <param name="pageSize">The number of users per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the list of users and the total count.</returns>
    public async Task<(List<UserAdminDto> Users, int TotalCount)> ListUsersAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page < 1)
        {
            throw new ArgumentException("Page must be 1 or greater.", nameof(page));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));
        }

        var skip = (page - 1) * pageSize;
        var (users, totalCount) = await _userRepository.ListUsersAsync(skip, pageSize, ct);

        var userDtos = users.Select(u => new UserAdminDto(
            u.Id,
            u.Username,
            u.DisplayName,
            u.Email,
            u.Role,
            u.CreatedAt)).ToList();

        return (userDtos, totalCount);
    }

    /// <summary>
    /// Promotes a Standard user to Admin role.
    /// Records an AuditEvent for the promotion.
    /// </summary>
    /// <param name="userId">The ID of the user to promote.</param>
    /// <param name="actingUserId">The ID of the admin performing the promotion.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user DTO.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the user is already an Admin.</exception>
    public async Task<UserAdminDto> PromoteAsync(Guid userId, Guid actingUserId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (actingUserId == Guid.Empty)
        {
            throw new ArgumentException("Acting user ID cannot be empty.", nameof(actingUserId));
        }

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            _logger.LogWarning("Promote attempted for non-existent user {UserId}", userId);
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        if (user.Role == UserRole.Admin)
        {
            _logger.LogWarning("Promote attempted for user {UserId} who is already Admin", userId);
            throw new InvalidOperationException("User is already an Admin.");
        }

        user.Role = UserRole.Admin;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var updatedUser = await _userRepository.UpdateAsync(user, ct);

        // Record audit event
        await _auditService.RecordAsync(
            "UserPromoted",
            actingUserId,
            "User",
            userId,
            AuditOutcome.Success,
            "0.0.0.0",
            ct);

        _logger.LogInformation("User {UserId} promoted to Admin by {ActingUserId}", userId, actingUserId);

        return new UserAdminDto(
            updatedUser.Id,
            updatedUser.Username,
            updatedUser.DisplayName,
            updatedUser.Email,
            updatedUser.Role,
            updatedUser.CreatedAt);
    }

    /// <summary>
    /// Demotes an Admin user to Standard role.
    /// Prevents demotion of the last admin in the system.
    /// Records an AuditEvent for the demotion.
    /// </summary>
    /// <param name="userId">The ID of the user to demote.</param>
    /// <param name="actingUserId">The ID of the admin performing the demotion.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user DTO.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the user is already Standard, or if this is the last admin.</exception>
    public async Task<UserAdminDto> DemoteAsync(Guid userId, Guid actingUserId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (actingUserId == Guid.Empty)
        {
            throw new ArgumentException("Acting user ID cannot be empty.", nameof(actingUserId));
        }

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            _logger.LogWarning("Demote attempted for non-existent user {UserId}", userId);
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        if (user.Role == UserRole.Standard)
        {
            _logger.LogWarning("Demote attempted for user {UserId} who is already Standard", userId);
            throw new InvalidOperationException("User is already Standard.");
        }

        // Check if this is the last admin
        var adminCount = await _userRepository.CountAdminsAsync(ct);
        if (adminCount <= 1)
        {
            _logger.LogWarning("Demote attempted for user {UserId} but this is the last admin", userId);
            throw new InvalidOperationException("Cannot demote the last admin in the system.");
        }

        user.Role = UserRole.Standard;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var updatedUser = await _userRepository.UpdateAsync(user, ct);

        // Record audit event
        await _auditService.RecordAsync(
            "UserDemoted",
            actingUserId,
            "User",
            userId,
            AuditOutcome.Success,
            "0.0.0.0",
            ct);

        _logger.LogInformation("User {UserId} demoted to Standard by {ActingUserId}", userId, actingUserId);

        return new UserAdminDto(
            updatedUser.Id,
            updatedUser.Username,
            updatedUser.DisplayName,
            updatedUser.Email,
            updatedUser.Role,
            updatedUser.CreatedAt);
    }

    /// <summary>
    /// Reassigns ownership of a resource (Location or LocationCollection) to a new owner.
    /// Records an AuditEvent for the reassignment.
    /// </summary>
    /// <param name="resourceType">The type of resource: "Location" or "LocationCollection".</param>
    /// <param name="resourceId">The ID of the resource to reassign.</param>
    /// <param name="newOwnerId">The ID of the new owner user.</param>
    /// <param name="actingUserId">The ID of the admin performing the reassignment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A DTO representing the updated resource.</returns>
    /// <exception cref="ArgumentException">Thrown if resourceType is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the resource or new owner is not found.</exception>
    public async Task<object> ReassignResourceOwnershipAsync(
        string resourceType,
        Guid resourceId,
        Guid newOwnerId,
        Guid actingUserId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ArgumentException("Resource type cannot be empty.", nameof(resourceType));
        }

        if (resourceId == Guid.Empty)
        {
            throw new ArgumentException("Resource ID cannot be empty.", nameof(resourceId));
        }

        if (newOwnerId == Guid.Empty)
        {
            throw new ArgumentException("New owner ID cannot be empty.", nameof(newOwnerId));
        }

        if (actingUserId == Guid.Empty)
        {
            throw new ArgumentException("Acting user ID cannot be empty.", nameof(actingUserId));
        }

        // Validate new owner exists
        var newOwner = await _userRepository.GetByIdAsync(newOwnerId, ct);
        if (newOwner == null)
        {
            _logger.LogWarning("Reassignment attempted for non-existent new owner {NewOwnerId}", newOwnerId);
            throw new KeyNotFoundException($"New owner with ID {newOwnerId} not found.");
        }

        // Handle based on resource type
        if (resourceType.Equals("Location", StringComparison.OrdinalIgnoreCase))
        {
            return await ReassignLocationOwnershipAsync(resourceId, newOwnerId, actingUserId, ct);
        }
        else if (resourceType.Equals("LocationCollection", StringComparison.OrdinalIgnoreCase))
        {
            return await ReassignCollectionOwnershipAsync(resourceId, newOwnerId, actingUserId, ct);
        }
        else
        {
            throw new ArgumentException($"Invalid resource type '{resourceType}'. Must be 'Location' or 'LocationCollection'.", nameof(resourceType));
        }
    }

    /// <summary>
    /// Reassigns ownership of a Location to a new owner.
    /// </summary>
    private async Task<object> ReassignLocationOwnershipAsync(
        Guid locationId,
        Guid newOwnerId,
        Guid actingUserId,
        CancellationToken ct)
    {
        var location = await _locationRepository.GetByIdAsync(locationId, ct);
        if (location == null)
        {
            _logger.LogWarning("Reassignment attempted for non-existent location {LocationId}", locationId);
            throw new KeyNotFoundException($"Location with ID {locationId} not found.");
        }

        var oldOwnerId = location.CreatorId;

        // Create a new Location instance with updated CreatorId (since CreatorId is init-only)
        var updatedLocation = new Location
        {
            Id = location.Id,
            Name = location.Name,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Coordinates = location.Coordinates,
            SourceSrid = location.SourceSrid,
            ContentSequence = location.ContentSequence,
            CreatorId = newOwnerId,
            CreatedAt = location.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = await _locationRepository.UpdateAsync(updatedLocation, ct);

        // Record audit event
        await _auditService.RecordAsync(
            "ResourceOwnershipReassigned",
            actingUserId,
            "Location",
            locationId,
            AuditOutcome.Success,
            "0.0.0.0",
            ct);

        _logger.LogInformation(
            "Location {LocationId} ownership reassigned from {OldOwnerId} to {NewOwnerId} by {ActingUserId}",
            locationId, oldOwnerId, newOwnerId, actingUserId);

        return new
        {
            id = result.Id,
            name = result.Name,
            creatorId = result.CreatorId,
            latitude = result.Latitude,
            longitude = result.Longitude,
            sourceSrid = result.SourceSrid,
            createdAt = result.CreatedAt,
            updatedAt = result.UpdatedAt
        };
    }

    /// <summary>
    /// Reassigns ownership of a LocationCollection to a new owner.
    /// </summary>
    private async Task<object> ReassignCollectionOwnershipAsync(
        Guid collectionId,
        Guid newOwnerId,
        Guid actingUserId,
        CancellationToken ct)
    {
        var collection = await _collectionRepository.GetByIdAsync(collectionId, ct);
        if (collection == null)
        {
            _logger.LogWarning("Reassignment attempted for non-existent collection {CollectionId}", collectionId);
            throw new KeyNotFoundException($"LocationCollection with ID {collectionId} not found.");
        }

        var oldOwnerId = collection.OwnerId;

        // Create a new LocationCollection instance with updated OwnerId (since OwnerId is init-only)
        var updatedCollection = new LocationCollection
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            OwnerId = newOwnerId,
            ThumbnailImageId = collection.ThumbnailImageId,
            BoundingShapeId = collection.BoundingShapeId,
            IsPublic = collection.IsPublic,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = await _collectionRepository.UpdateAsync(updatedCollection, ct);

        // Record audit event
        await _auditService.RecordAsync(
            "ResourceOwnershipReassigned",
            actingUserId,
            "LocationCollection",
            collectionId,
            AuditOutcome.Success,
            "0.0.0.0",
            ct);

        _logger.LogInformation(
            "LocationCollection {CollectionId} ownership reassigned from {OldOwnerId} to {NewOwnerId} by {ActingUserId}",
            collectionId, oldOwnerId, newOwnerId, actingUserId);

        return new
        {
            id = result.Id,
            name = result.Name,
            ownerId = result.OwnerId,
            description = result.Description,
            isPublic = result.IsPublic,
            createdAt = result.CreatedAt,
            updatedAt = result.UpdatedAt
        };
    }
}
