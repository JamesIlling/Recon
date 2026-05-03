using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository interface for Location entity CRUD operations and spatial queries.
/// </summary>
public interface ILocationRepository
{
    /// <summary>
    /// Creates a new Location in the database.
    /// </summary>
    /// <param name="location">The Location entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created Location with its ID populated.</returns>
    Task<Location> CreateAsync(Location location, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a Location by its ID.
    /// </summary>
    /// <param name="id">The Location ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Location if found; otherwise null.</returns>
    Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a paginated list of Locations in descending order of creation timestamp.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the total count and the list of Locations for the page.</returns>
    Task<(int TotalCount, List<Location> Items)> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing Location.
    /// </summary>
    /// <param name="location">The Location entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated Location.</returns>
    Task<Location> UpdateAsync(Location location, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a Location by its ID.
    /// </summary>
    /// <param name="id">The Location ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the Location was deleted; false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all Locations created by a specific user.
    /// </summary>
    /// <param name="creatorId">The creator's User ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of Locations created by the user.</returns>
    Task<List<Location>> GetByCreatorIdAsync(Guid creatorId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a Location exists by its ID.
    /// </summary>
    /// <param name="id">The Location ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the Location exists; otherwise false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
