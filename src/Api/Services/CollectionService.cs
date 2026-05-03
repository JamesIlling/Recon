using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocationManagement.Api.Services;

/// <summary>
/// Service implementation for managing LocationCollections.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CollectionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionService"/> class.
    /// </summary>
    public CollectionService(
        ICollectionRepository collectionRepository,
        AppDbContext context,
        IAuditService auditService,
        INotificationService notificationService,
        IImageProcessingService imageProcessingService,
        ICacheService cacheService,
        ILogger<CollectionService> logger)
    {
        _collectionRepository = collectionRepository;
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
        _imageProcessingService = imageProcessingService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LocationCollection> CreateAsync(
        string name,
        string? description,
        bool isPublic,
        Guid? boundingShapeId,
        Guid? thumbnailImageId,
        Guid ownerId,
        string sourceIp,
        CancellationToken ct)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Collection name is required.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Collection name must not exceed 200 characters.", nameof(name));

        if (description != null && description.Length > 1000)
            throw new ArgumentException("Collection description must not exceed 1000 characters.", nameof(description));

        // Validate bounding shape exists if provided
        if (boundingShapeId.HasValue)
        {
            var shapeExists = await _context.NamedShapes.AnyAsync(ns => ns.Id == boundingShapeId.Value, ct);
            if (!shapeExists)
                throw new ArgumentException("The specified bounding shape does not exist.", nameof(boundingShapeId));
        }

        // Validate thumbnail image exists if provided
        if (thumbnailImageId.HasValue)
        {
            var imageExists = await _context.Images.AnyAsync(i => i.Id == thumbnailImageId.Value, ct);
            if (!imageExists)
                throw new ArgumentException("The specified thumbnail image does not exist.", nameof(thumbnailImageId));
        }

        var now = DateTimeOffset.UtcNow;
        var collection = new LocationCollection
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            OwnerId = ownerId,
            IsPublic = isPublic,
            BoundingShapeId = boundingShapeId,
            ThumbnailImageId = thumbnailImageId,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _collectionRepository.CreateAsync(collection, ct);

        // Record audit event
        await _auditService.RecordAsync(
            "CollectionCreated",
            ownerId,
            "LocationCollection",
            created.Id,
            AuditOutcome.Success,
            sourceIp,
            ct);

        // Invalidate public collections cache
        await _cacheService.InvalidateByPrefixAsync("collections:public:", ct);

        _logger.LogInformation("Collection created: {CollectionId} by user {UserId}", created.Id, ownerId);

        return created;
    }

    /// <inheritdoc />
    public async Task<LocationCollection?> GetByIdAsync(Guid id, Guid? requestingUserId, CancellationToken ct)
    {
        var collection = await _collectionRepository.GetByIdAsync(id, ct);

        if (collection == null)
            return null;

        // Enforce private visibility at service layer
        if (!collection.IsPublic && collection.OwnerId != requestingUserId)
            return null;

        return collection;
    }

    /// <inheritdoc />
    public async Task<(int TotalCount, List<LocationCollection> Items)> ListPublicAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page < 1)
            throw new ArgumentException("Page must be >= 1.", nameof(page));

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

        var cacheKey = $"collections:public:{page}:{pageSize}";
        var cached = await _cacheService.GetAsync<CollectionListCacheEntry>(cacheKey, ct);
        if (cached != null)
            return (cached.TotalCount, cached.Items);

        var result = await _collectionRepository.ListPublicAsync(page, pageSize, ct);

        // Cache for 60 seconds
        var cacheEntry = new CollectionListCacheEntry { TotalCount = result.TotalCount, Items = result.Items };
        await _cacheService.SetAsync(cacheKey, cacheEntry, TimeSpan.FromSeconds(60), ct);

        return result;
    }

    /// <inheritdoc />
    public async Task<(int TotalCount, List<(LocationCollection Collection, bool IsOwner)> Items)> ListCombinedAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (page < 1)
            throw new ArgumentException("Page must be >= 1.", nameof(page));

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

        // Never cache combined list (user-specific)
        var (totalCount, items) = await _collectionRepository.ListCombinedAsync(userId, page, pageSize, ct);

        var result = items.Select(c => (c, IsOwner: c.OwnerId == userId)).ToList();

        return (totalCount, result);
    }

    /// <inheritdoc />
    public async Task<LocationCollection> UpdateAsync(
        Guid id,
        string name,
        string? description,
        bool isPublic,
        Guid? boundingShapeId,
        Guid? thumbnailImageId,
        Guid userId,
        string sourceIp,
        CancellationToken ct)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Collection name is required.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Collection name must not exceed 200 characters.", nameof(name));

        if (description != null && description.Length > 1000)
            throw new ArgumentException("Collection description must not exceed 1000 characters.", nameof(description));

        var collection = await _collectionRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Collection not found.");

        // Enforce owner-only access
        if (collection.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the collection owner can update this collection.");

        // Validate bounding shape exists if provided
        if (boundingShapeId.HasValue)
        {
            var shapeExists = await _context.NamedShapes.AnyAsync(ns => ns.Id == boundingShapeId.Value, ct);
            if (!shapeExists)
                throw new ArgumentException("The specified bounding shape does not exist.", nameof(boundingShapeId));
        }

        // Validate thumbnail image exists if provided
        if (thumbnailImageId.HasValue)
        {
            var imageExists = await _context.Images.AnyAsync(i => i.Id == thumbnailImageId.Value, ct);
            if (!imageExists)
                throw new ArgumentException("The specified thumbnail image does not exist.", nameof(thumbnailImageId));
        }

        // Update collection
        collection.Name = name;
        collection.Description = description;
        collection.IsPublic = isPublic;
        collection.BoundingShapeId = boundingShapeId;
        collection.ThumbnailImageId = thumbnailImageId;
        collection.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await _collectionRepository.UpdateAsync(collection, ct);

        // Record audit event
        await _auditService.RecordAsync(
            "CollectionUpdated",
            userId,
            "LocationCollection",
            id,
            AuditOutcome.Success,
            sourceIp,
            ct);

        // Invalidate caches
        await _cacheService.InvalidateAsync($"collections:detail:{id}", ct);
        await _cacheService.InvalidateByPrefixAsync("collections:public:", ct);

        _logger.LogInformation("Collection updated: {CollectionId} by user {UserId}", id, userId);

        return updated;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, Guid userId, bool isAdmin, string sourceIp, CancellationToken ct)
    {
        var collection = await _collectionRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Collection not found.");

        // Enforce owner or admin access
        if (collection.OwnerId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Only the collection owner or an admin can delete this collection.");

        // Delete orphaned thumbnail image if it exists and is not referenced elsewhere
        if (collection.ThumbnailImageId.HasValue)
        {
            var imageRefCount = await _context.LocationCollections
                .CountAsync(c => c.ThumbnailImageId == collection.ThumbnailImageId.Value, ct);

            if (imageRefCount == 1) // Only this collection references it
            {
                await _imageProcessingService.DeleteImageAndVariantsAsync(collection.ThumbnailImageId.Value, ct);
            }
        }

        // Delete collection (cascade deletes CollectionMembers and PendingMembershipRequests)
        await _collectionRepository.DeleteAsync(id, ct);

        // Record audit event
        await _auditService.RecordAsync(
            "CollectionDeleted",
            userId,
            "LocationCollection",
            id,
            AuditOutcome.Success,
            sourceIp,
            ct);

        // Invalidate caches
        await _cacheService.InvalidateAsync($"collections:detail:{id}", ct);
        await _cacheService.InvalidateByPrefixAsync("collections:public:", ct);

        _logger.LogInformation("Collection deleted: {CollectionId} by user {UserId}", id, userId);
    }

    /// <inheritdoc />
    public async Task<object> AddMemberAsync(Guid collectionId, Guid locationId, Guid requestingUserId, string sourceIp, CancellationToken ct)
    {
        var collection = await _collectionRepository.GetByIdAsync(collectionId, ct)
            ?? throw new InvalidOperationException("Collection not found.");

        var location = await _context.Locations.FindAsync(new object[] { locationId }, cancellationToken: ct)
            ?? throw new InvalidOperationException("Location not found.");

        // Check if location is already a member
        var existingMember = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.CollectionId == collectionId && cm.LocationId == locationId, ct);

        if (existingMember != null)
            throw new InvalidOperationException("Location is already a member of this collection.");

        // If requesting user is the owner, add directly
        if (collection.OwnerId == requestingUserId)
        {
            var member = new CollectionMember
            {
                LocationId = locationId,
                CollectionId = collectionId,
                AddedAt = DateTimeOffset.UtcNow
            };

            _context.CollectionMembers.Add(member);
            await _context.SaveChangesAsync(ct);

            // Invalidate cache
            await _cacheService.InvalidateAsync($"collections:detail:{collectionId}", ct);

            _logger.LogInformation("Location {LocationId} added to collection {CollectionId} by owner {UserId}", locationId, collectionId, requestingUserId);

            return member;
        }

        // Otherwise, create a pending membership request
        var request = new PendingMembershipRequest
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            LocationId = locationId,
            RequestedByUserId = requestingUserId,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = MembershipRequestStatus.Pending
        };

        _context.PendingMembershipRequests.Add(request);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Pending membership request created: {RequestId} for location {LocationId} in collection {CollectionId} by user {UserId}", request.Id, locationId, collectionId, requestingUserId);

        return request;
    }

    /// <inheritdoc />
    public async Task RemoveMemberAsync(Guid collectionId, Guid locationId, Guid userId, string sourceIp, CancellationToken ct)
    {
        var collection = await _collectionRepository.GetByIdAsync(collectionId, ct)
            ?? throw new InvalidOperationException("Collection not found.");

        // Enforce owner-only access
        if (collection.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the collection owner can remove members.");

        var member = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.CollectionId == collectionId && cm.LocationId == locationId, ct)
            ?? throw new InvalidOperationException("Location is not a member of this collection.");

        _context.CollectionMembers.Remove(member);
        await _context.SaveChangesAsync(ct);

        // Invalidate cache
        await _cacheService.InvalidateAsync($"collections:detail:{collectionId}", ct);

        _logger.LogInformation("Location {LocationId} removed from collection {CollectionId} by user {UserId}", locationId, collectionId, userId);
    }

    /// <inheritdoc />
    public async Task ApproveMembershipAsync(Guid collectionId, Guid requestId, Guid userId, string sourceIp, CancellationToken ct)
    {
        var collection = await _collectionRepository.GetByIdAsync(collectionId, ct)
            ?? throw new InvalidOperationException("Collection not found.");

        // Enforce owner-only access
        if (collection.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the collection owner can approve membership requests.");

        var request = await _context.PendingMembershipRequests
            .FirstOrDefaultAsync(pmr => pmr.Id == requestId && pmr.CollectionId == collectionId, ct)
            ?? throw new InvalidOperationException("Membership request not found.");

        // Add location to collection
        var member = new CollectionMember
        {
            LocationId = request.LocationId,
            CollectionId = collectionId,
            AddedAt = DateTimeOffset.UtcNow
        };

        _context.CollectionMembers.Add(member);

        // Update request status
        request.Status = MembershipRequestStatus.Approved;

        await _context.SaveChangesAsync(ct);

        // Create notification for requester
        await _notificationService.NotifyMembershipApprovedAsync(
            request.RequestedByUserId,
            collectionId,
            collection.Name,
            ct);

        // Record audit event
        await _auditService.RecordAsync(
            "MembershipApproved",
            userId,
            "PendingMembershipRequest",
            requestId,
            AuditOutcome.Success,
            sourceIp,
            ct);

        // Invalidate cache
        await _cacheService.InvalidateAsync($"collections:detail:{collectionId}", ct);

        _logger.LogInformation("Membership request {RequestId} approved by user {UserId}", requestId, userId);
    }

    /// <inheritdoc />
    public async Task RejectMembershipAsync(Guid collectionId, Guid requestId, Guid userId, string sourceIp, CancellationToken ct)
    {
        var collection = await _collectionRepository.GetByIdAsync(collectionId, ct)
            ?? throw new InvalidOperationException("Collection not found.");

        // Enforce owner-only access
        if (collection.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the collection owner can reject membership requests.");

        var request = await _context.PendingMembershipRequests
            .FirstOrDefaultAsync(pmr => pmr.Id == requestId && pmr.CollectionId == collectionId, ct)
            ?? throw new InvalidOperationException("Membership request not found.");

        var requesterUserId = request.RequestedByUserId;

        // Delete request
        _context.PendingMembershipRequests.Remove(request);
        await _context.SaveChangesAsync(ct);

        // Create notification for requester
        await _notificationService.NotifyMembershipRejectedAsync(
            requesterUserId,
            collectionId,
            collection.Name,
            ct);

        // Record audit event
        await _auditService.RecordAsync(
            "MembershipRejected",
            userId,
            "PendingMembershipRequest",
            requestId,
            AuditOutcome.Success,
            sourceIp,
            ct);

        _logger.LogInformation("Membership request {RequestId} rejected by user {UserId}", requestId, userId);
    }

    /// <inheritdoc />
    public async Task<List<PendingMembershipRequest>> GetPendingMembershipsAsync(Guid collectionId, Guid userId, CancellationToken ct)
    {
        var collection = await _collectionRepository.GetByIdAsync(collectionId, ct)
            ?? throw new InvalidOperationException("Collection not found.");

        // Enforce owner-only access
        if (collection.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the collection owner can view pending membership requests.");

        return await _context.PendingMembershipRequests
            .Where(pmr => pmr.CollectionId == collectionId && pmr.Status == MembershipRequestStatus.Pending)
            .Include(pmr => pmr.RequestedByUser)
            .Include(pmr => pmr.Location)
            .OrderByDescending(pmr => pmr.RequestedAt)
            .ToListAsync(ct);
    }
}


/// <summary>
/// Cache entry for collection list results.
/// </summary>
internal class CollectionListCacheEntry
{
    /// <summary>
    /// Gets or sets the total count of collections.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the list of collections.
    /// </summary>
    public List<LocationCollection> Items { get; set; } = [];
}
