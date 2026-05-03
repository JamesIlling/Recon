using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;

namespace LocationManagement.Api.Services;

/// <summary>
/// Defines operations for managing LocationCollections, including creation, retrieval, editing, and membership workflows.
/// </summary>
public interface ICollectionService
{
    /// <summary>
    /// Creates a new LocationCollection with the provided data.
    /// Validates input, persists the collection, and records an audit event.
    /// </summary>
    /// <param name="name">The collection name (max 200 characters).</param>
    /// <param name="description">Optional collection description (max 1000 characters).</param>
    /// <param name="isPublic">Whether the collection is public or private.</param>
    /// <param name="boundingShapeId">Optional ID of a NamedShape to use as the bounding shape.</param>
    /// <param name="thumbnailImageId">Optional ID of an image to use as the collection thumbnail.</param>
    /// <param name="ownerId">The ID of the user creating the collection.</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created LocationCollection entity.</returns>
    /// <exception cref="ArgumentException">Thrown if validation fails.</exception>
    Task<LocationCollection> CreateAsync(
        string name,
        string? description,
        bool isPublic,
        Guid? boundingShapeId,
        Guid? thumbnailImageId,
        Guid ownerId,
        string sourceIp,
        CancellationToken ct);

    /// <summary>
    /// Retrieves a LocationCollection by ID.
    /// Enforces private visibility at the service layer: private collections are only accessible to the owner.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="requestingUserId">The ID of the requesting user (null if unauthenticated).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The LocationCollection entity, or null if not found or not accessible.</returns>
    Task<LocationCollection?> GetByIdAsync(Guid id, Guid? requestingUserId, CancellationToken ct);

    /// <summary>
    /// Retrieves a paginated list of public LocationCollections in descending order of creation timestamp.
    /// </summary>
    /// <param name="page">The page number (1-indexed).</param>
    /// <param name="pageSize">The number of items per page (1–100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple of (total count, list of public collections).</returns>
    /// <exception cref="ArgumentException">Thrown if page or pageSize is invalid.</exception>
    Task<(int TotalCount, List<LocationCollection> Items)> ListPublicAsync(int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Retrieves a paginated list of public collections and collections owned by the requesting user,
    /// with an isOwner flag indicating ownership. Never cached.
    /// </summary>
    /// <param name="userId">The ID of the requesting user.</param>
    /// <param name="page">The page number (1-indexed).</param>
    /// <param name="pageSize">The number of items per page (1–100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple of (total count, list of collections with ownership info).</returns>
    /// <exception cref="ArgumentException">Thrown if page or pageSize is invalid.</exception>
    Task<(int TotalCount, List<(LocationCollection Collection, bool IsOwner)> Items)> ListCombinedAsync(Guid userId, int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Updates a LocationCollection (owner only).
    /// Validates input, validates NamedShape reference, and records an audit event.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="name">The updated collection name.</param>
    /// <param name="description">The updated description (nullable).</param>
    /// <param name="isPublic">The updated visibility.</param>
    /// <param name="boundingShapeId">The updated bounding shape ID (nullable).</param>
    /// <param name="thumbnailImageId">The updated thumbnail image ID (nullable).</param>
    /// <param name="userId">The ID of the user performing the update (must be the owner).</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated LocationCollection entity.</returns>
    /// <exception cref="ArgumentException">Thrown if validation fails.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the owner.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the collection is not found.</exception>
    Task<LocationCollection> UpdateAsync(
        Guid id,
        string name,
        string? description,
        bool isPublic,
        Guid? boundingShapeId,
        Guid? thumbnailImageId,
        Guid userId,
        string sourceIp,
        CancellationToken ct);

    /// <summary>
    /// Deletes a LocationCollection (owner or admin only).
    /// Cascade deletes CollectionMembers and orphaned images. Records an audit event.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="userId">The ID of the user deleting (must be the owner or admin).</param>
    /// <param name="isAdmin">Whether the user is an admin.</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the owner or admin.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the collection is not found.</exception>
    Task DeleteAsync(Guid id, Guid userId, bool isAdmin, string sourceIp, CancellationToken ct);

    /// <summary>
    /// Adds a Location to a LocationCollection.
    /// If the requesting user is the owner, the location is added directly.
    /// If the requesting user is not the owner, a pending membership request is created.
    /// Creates a notification when a pending request is approved or rejected.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="locationId">The location ID to add.</param>
    /// <param name="requestingUserId">The ID of the user requesting to add the location.</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created CollectionMember or PendingMembershipRequest.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the collection or location is not found.</exception>
    Task<object> AddMemberAsync(Guid collectionId, Guid locationId, Guid requestingUserId, string sourceIp, CancellationToken ct);

    /// <summary>
    /// Removes a Location from a LocationCollection (owner only).
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="locationId">The location ID to remove.</param>
    /// <param name="userId">The ID of the user performing the removal (must be the owner).</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the owner.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the collection or membership is not found.</exception>
    Task RemoveMemberAsync(Guid collectionId, Guid locationId, Guid userId, string sourceIp, CancellationToken ct);

    /// <summary>
    /// Approves a pending membership request (owner only).
    /// Adds the location to the collection, creates a notification, and records an audit event.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="requestId">The pending membership request ID.</param>
    /// <param name="userId">The ID of the user approving (must be the owner).</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the owner.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the collection or request is not found.</exception>
    Task ApproveMembershipAsync(Guid collectionId, Guid requestId, Guid userId, string sourceIp, CancellationToken ct);

    /// <summary>
    /// Rejects a pending membership request (owner only).
    /// Deletes the request, creates a notification, and records an audit event.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="requestId">The pending membership request ID.</param>
    /// <param name="userId">The ID of the user rejecting (must be the owner).</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the owner.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the collection or request is not found.</exception>
    Task RejectMembershipAsync(Guid collectionId, Guid requestId, Guid userId, string sourceIp, CancellationToken ct);

    /// <summary>
    /// Retrieves all pending membership requests for a collection (owner only).
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="userId">The ID of the requesting user (must be the owner).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of pending membership requests.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the owner.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the collection is not found.</exception>
    Task<List<PendingMembershipRequest>> GetPendingMembershipsAsync(Guid collectionId, Guid userId, CancellationToken ct);
}
