using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Services;

/// <summary>
/// Service interface for NamedShape operations including upload, rename, delete, and list.
/// All write operations are admin-only.
/// </summary>
public interface INamedShapeService
{
    /// <summary>
    /// Uploads a new NamedShape from GeoJSON geometry.
    /// Admin-only operation.
    /// </summary>
    /// <param name="name">The name of the shape (max 200 characters, must be unique).</param>
    /// <param name="geoJsonGeometry">The GeoJSON geometry string (Polygon or MultiPolygon).</param>
    /// <param name="adminUserId">The ID of the admin user performing the upload.</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created NamedShape.</returns>
    /// <exception cref="ArgumentException">Thrown when name is empty, geometry is invalid, or vertex count exceeds 1000.</exception>
    /// <exception cref="InvalidOperationException">Thrown when name is not unique.</exception>
    Task<NamedShape> UploadAsync(string name, string geoJsonGeometry, Guid adminUserId, string sourceIp, CancellationToken cancellationToken);

    /// <summary>
    /// Renames an existing NamedShape.
    /// Admin-only operation.
    /// </summary>
    /// <param name="id">The ID of the NamedShape to rename.</param>
    /// <param name="newName">The new name (max 200 characters, must be unique).</param>
    /// <param name="adminUserId">The ID of the admin user performing the rename.</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated NamedShape.</returns>
    /// <exception cref="ArgumentException">Thrown when newName is empty or exceeds max length.</exception>
    /// <exception cref="InvalidOperationException">Thrown when NamedShape not found or new name is not unique.</exception>
    Task<NamedShape> RenameAsync(Guid id, string newName, Guid adminUserId, string sourceIp, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a NamedShape.
    /// Admin-only operation. Fails if the shape is referenced by any LocationCollection.
    /// </summary>
    /// <param name="id">The ID of the NamedShape to delete.</param>
    /// <param name="adminUserId">The ID of the admin user performing the delete.</param>
    /// <param name="sourceIp">The source IP address for audit logging.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when NamedShape not found or is referenced by a collection.</exception>
    Task DeleteAsync(Guid id, Guid adminUserId, string sourceIp, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all NamedShapes with pagination.
    /// Authenticated users only. Returns id and name only (no geometry).
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the total count and the list of NamedShapes for the page.</returns>
    Task<(int TotalCount, List<(Guid Id, string Name)> Items)> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
