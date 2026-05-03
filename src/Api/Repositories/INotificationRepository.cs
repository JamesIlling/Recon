using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository interface for Notification entity operations.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Creates a new Notification in the database.
    /// </summary>
    /// <param name="notification">The Notification entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created Notification.</returns>
    Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a Notification by its ID.
    /// </summary>
    /// <param name="id">The Notification ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Notification if found; otherwise null.</returns>
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all Notifications for a specific user.
    /// </summary>
    /// <param name="userId">The User ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of Notifications for the user.</returns>
    Task<List<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a Notification by its ID.
    /// </summary>
    /// <param name="id">The Notification ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the Notification was deleted; false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a Notification exists by its ID.
    /// </summary>
    /// <param name="id">The Notification ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the Notification exists; otherwise false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing Notification in the database.
    /// </summary>
    /// <param name="notification">The Notification entity to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated Notification.</returns>
    Task<Notification> UpdateAsync(Notification notification, CancellationToken cancellationToken);
}
