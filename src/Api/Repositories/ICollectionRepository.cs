using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository interface for LocationCollection entity CRUD operations.
/// </summary>
public interface ICollectionRepository
{
    /// <summary>
    /// Creates a new LocationCollection in the database.
    /// </summary>
    /// <param name="collection">The LocationCollection entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created LocationCollection with its ID populated.</returns>
    Task<LocationCollection> CreateAsync(LocationCollection collection, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a LocationCollection by its ID.
    /// </summary>
    /// <param name="id">The LocationCollection ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The LocationCollection if found; otherwise null.</returns>
    Task<LocationCollection?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a paginated list of public LocationCollections in descending order of creation timestamp.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the total count and the list of public LocationCollections for the page.</returns>
    Task<(int TotalCount, List<LocationCollection> Items)> ListPublicAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a paginated list of public collections and collections owned by a specific user,
    /// in descending order of creation timestamp.
    /// </summary>
    /// <param name="userId">The ID of the user (for filtering owned collections).</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the total count and the list of collections for the page.</returns>
    Task<(int TotalCount, List<LocationCollection> Items)> ListCombinedAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing LocationCollection.
    /// </summary>
    /// <param name="collection">The LocationCollection entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated LocationCollection.</returns>
    Task<LocationCollection> UpdateAsync(LocationCollection collection, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a LocationCollection by its ID.
    /// </summary>
    /// <param name="id">The LocationCollection ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the LocationCollection was deleted; false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all LocationCollections owned by a specific user.
    /// </summary>
    /// <param name="ownerId">The owner's User ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of LocationCollections owned by the user.</returns>
    Task<List<LocationCollection>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a LocationCollection exists by its ID.
    /// </summary>
    /// <param name="id">The LocationCollection ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the LocationCollection exists; otherwise false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a LocationCollection is referenced by a NamedShape (i.e., uses it as BoundingShape).
    /// </summary>
    /// <param name="namedShapeId">The NamedShape ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if any collection references this shape; otherwise false.</returns>
    Task<bool> IsNamedShapeReferencedAsync(Guid namedShapeId, CancellationToken cancellationToken);
}
