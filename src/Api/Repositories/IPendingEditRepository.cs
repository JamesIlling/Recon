using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository interface for PendingEdit entity operations.
/// </summary>
public interface IPendingEditRepository
{
    /// <summary>
    /// Creates a new PendingEdit in the database.
    /// </summary>
    /// <param name="pendingEdit">The PendingEdit entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created PendingEdit.</returns>
    Task<PendingEdit> CreateAsync(PendingEdit pendingEdit, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a PendingEdit by its ID.
    /// </summary>
    /// <param name="id">The PendingEdit ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PendingEdit if found; otherwise null.</returns>
    Task<PendingEdit?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all PendingEdits for a specific Location.
    /// </summary>
    /// <param name="locationId">The Location ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of PendingEdits for the Location.</returns>
    Task<List<PendingEdit>> GetByLocationIdAsync(Guid locationId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a PendingEdit for a specific Location and submitting User.
    /// </summary>
    /// <param name="locationId">The Location ID.</param>
    /// <param name="submittedByUserId">The User ID who submitted the edit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PendingEdit if found; otherwise null.</returns>
    Task<PendingEdit?> GetByLocationAndUserAsync(Guid locationId, Guid submittedByUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing PendingEdit.
    /// </summary>
    /// <param name="pendingEdit">The PendingEdit entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated PendingEdit.</returns>
    Task<PendingEdit> UpdateAsync(PendingEdit pendingEdit, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a PendingEdit by its ID.
    /// </summary>
    /// <param name="id">The PendingEdit ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the PendingEdit was deleted; false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all PendingEdits for a specific Location.
    /// </summary>
    /// <param name="locationId">The Location ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of PendingEdits deleted.</returns>
    Task<int> DeleteByLocationIdAsync(Guid locationId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a PendingEdit exists by its ID.
    /// </summary>
    /// <param name="id">The PendingEdit ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the PendingEdit exists; otherwise false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
