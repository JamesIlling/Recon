using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository interface for NamedShape entity CRUD operations.
/// </summary>
public interface INamedShapeRepository
{
    /// <summary>
    /// Creates a new NamedShape in the database.
    /// </summary>
    /// <param name="namedShape">The NamedShape entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created NamedShape with its ID populated.</returns>
    Task<NamedShape> CreateAsync(NamedShape namedShape, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a NamedShape by its ID.
    /// </summary>
    /// <param name="id">The NamedShape ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The NamedShape if found; otherwise null.</returns>
    Task<NamedShape?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a NamedShape by its name (case-insensitive).
    /// </summary>
    /// <param name="name">The NamedShape name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The NamedShape if found; otherwise null.</returns>
    Task<NamedShape?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a paginated list of NamedShapes in descending order of creation timestamp.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the total count and the list of NamedShapes for the page.</returns>
    Task<(int TotalCount, List<NamedShape> Items)> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing NamedShape.
    /// </summary>
    /// <param name="namedShape">The NamedShape entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated NamedShape.</returns>
    Task<NamedShape> UpdateAsync(NamedShape namedShape, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a NamedShape by its ID.
    /// </summary>
    /// <param name="id">The NamedShape ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the NamedShape was deleted; false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a NamedShape is referenced by any LocationCollection.
    /// </summary>
    /// <param name="id">The NamedShape ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the NamedShape is referenced; otherwise false.</returns>
    Task<bool> IsReferencedByCollectionAsync(Guid id, CancellationToken cancellationToken);
}
