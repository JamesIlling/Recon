using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Services;

/// <summary>
/// Defines operations for managing Locations, including creation, retrieval, editing, and approval workflows.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Creates a new Location with the provided data.
    /// Validates input, reprojects coordinates to WGS84, rounds to 6 decimal places, and records an audit event.
    /// </summary>
    /// <param name="name">The location name (max 200 characters).</param>
    /// <param name="latitude">The latitude coordinate (-90 to 90).</param>
    /// <param name="longitude">The longitude coordinate (-180 to 180).</param>
    /// <param name="sourceSrid">The source coordinate reference system ID (default 4326).</param>
    /// <param name="contentSequenceJson">JSON-serialized ContentSequence (1–200 blocks).</param>
    /// <param name="creatorId">The ID of the user creating the location.</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created Location entity.</returns>
    /// <exception cref="ArgumentException">Thrown if validation fails.</exception>
    Task<Location> CreateAsync(
        string name,
        double latitude,
        double longitude,
        int sourceSrid,
        string contentSequenceJson,
        Guid creatorId,
        string sourceIp,
        CancellationToken ct);

    /// <summary>
    /// Retrieves a Location by ID.
    /// Returns the canonical version, or the PendingEdit if the requesting user is the submitter.
    /// </summary>
    /// <param name="id">The location ID.</param>
    /// <param name="requestingUserId">The ID of the requesting user (null if unauthenticated).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Location entity, or null if not found.</returns>
    Task<Location?> GetByIdAsync(Guid id, Guid? requestingUserId, CancellationToken ct);

    /// <summary>
    /// Retrieves a paginated list of all Locations in descending order of creation timestamp.
    /// Does not include the full ContentSequence.
    /// </summary>
    /// <param name="page">The page number (1-indexed).</param>
    /// <param name="pageSize">The number of items per page (1–100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple of (total count, list of locations).</returns>
    /// <exception cref="ArgumentException">Thrown if page or pageSize is invalid.</exception>
    Task<(int TotalCount, List<Location> Items)> ListAsync(int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Updates a Location (creator path).
    /// Validates input, reprojects coordinates, replaces the canonical version, cleans up orphaned images, and records an audit event.
    /// </summary>
    /// <param name="id">The location ID.</param>
    /// <param name="name">The updated location name.</param>
    /// <param name="latitude">The updated latitude.</param>
    /// <param name="longitude">The updated longitude.</param>
    /// <param name="sourceSrid">The updated source SRID.</param>
    /// <param name="contentSequenceJson">The updated ContentSequence JSON.</param>
    /// <param name="userId">The ID of the user performing the update (must be the creator).</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated Location entity.</returns>
    /// <exception cref="ArgumentException">Thrown if validation fails.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the creator.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the location is not found.</exception>
    Task<Location> UpdateAsync(
        Guid id,
        string name,
        double latitude,
        double longitude,
        int sourceSrid,
        string contentSequenceJson,
        Guid userId,
        string sourceIp,
        CancellationToken ct);

    /// <summary>
    /// Submits a pending edit for a Location (non-creator path).
    /// Validates input, reprojects coordinates, upserts the PendingEdit, creates a notification, and records an audit event.
    /// </summary>
    /// <param name="id">The location ID.</param>
    /// <param name="name">The proposed location name.</param>
    /// <param name="latitude">The proposed latitude.</param>
    /// <param name="longitude">The proposed longitude.</param>
    /// <param name="sourceSrid">The proposed source SRID.</param>
    /// <param name="contentSequenceJson">The proposed ContentSequence JSON.</param>
    /// <param name="submittedByUserId">The ID of the user submitting the edit.</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created or updated PendingEdit entity.</returns>
    /// <exception cref="ArgumentException">Thrown if validation fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the location is not found.</exception>
    Task<PendingEdit> SubmitPendingEditAsync(
        Guid id,
        string name,
        double latitude,
        double longitude,
        int sourceSrid,
        string contentSequenceJson,
        Guid submittedByUserId,
        string sourceIp,
        CancellationToken ct);

    /// <summary>
    /// Approves a pending edit, promoting it to the canonical version.
    /// Deletes the PendingEdit, creates a notification, and records an audit event.
    /// </summary>
    /// <param name="locationId">The location ID.</param>
    /// <param name="editId">The pending edit ID.</param>
    /// <param name="userId">The ID of the user approving (must be the location creator).</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated Location entity with the approved changes.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the location creator.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the location or pending edit is not found.</exception>
    Task<Location> ApprovePendingEditAsync(
        Guid locationId,
        Guid editId,
        Guid userId,
        string sourceIp,
        CancellationToken ct);

    /// <summary>
    /// Rejects a pending edit, deleting it and any orphaned images.
    /// Creates a notification and records an audit event.
    /// </summary>
    /// <param name="locationId">The location ID.</param>
    /// <param name="editId">The pending edit ID.</param>
    /// <param name="userId">The ID of the user rejecting (must be the location creator).</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the location creator.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the location or pending edit is not found.</exception>
    Task RejectPendingEditAsync(
        Guid locationId,
        Guid editId,
        Guid userId,
        string sourceIp,
        CancellationToken ct);

    /// <summary>
    /// Deletes a Location (creator or admin only).
    /// Cascade deletes PendingEdits, CollectionMembers, and orphaned images. Records an audit event.
    /// </summary>
    /// <param name="id">The location ID.</param>
    /// <param name="userId">The ID of the user deleting (must be the creator or admin).</param>
    /// <param name="isAdmin">Whether the user is an admin.</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the creator or admin.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the location is not found.</exception>
    Task DeleteAsync(Guid id, Guid userId, bool isAdmin, string sourceIp, CancellationToken ct);

    /// <summary>
    /// Retrieves all pending edits for a Location (creator only).
    /// </summary>
    /// <param name="locationId">The location ID.</param>
    /// <param name="userId">The ID of the requesting user (must be the location creator).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of PendingEdit entities.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the location creator.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the location is not found.</exception>
    Task<List<PendingEdit>> GetPendingEditsAsync(Guid locationId, Guid userId, CancellationToken ct);
}
