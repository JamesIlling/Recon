using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository interface for Image entity CRUD operations.
/// </summary>
public interface IImageRepository
{
    /// <summary>
    /// Creates a new image record in the database.
    /// </summary>
    /// <param name="image">The image entity to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created image entity.</returns>
    Task<Image> CreateAsync(Image image, CancellationToken ct);

    /// <summary>
    /// Retrieves an image by its identifier.
    /// </summary>
    /// <param name="imageId">The image identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The image entity, or null if not found.</returns>
    Task<Image?> GetByIdAsync(Guid imageId, CancellationToken ct);

    /// <summary>
    /// Retrieves all images uploaded by a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of image entities.</returns>
    Task<IEnumerable<Image>> GetByUserIdAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Deletes an image record from the database.
    /// </summary>
    /// <param name="imageId">The image identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the image was deleted; false if not found.</returns>
    Task<bool> DeleteAsync(Guid imageId, CancellationToken ct);

    /// <summary>
    /// Updates an existing image record.
    /// </summary>
    /// <param name="image">The image entity with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated image entity.</returns>
    Task<Image> UpdateAsync(Image image, CancellationToken ct);

    /// <summary>
    /// Checks if an image with the given identifier exists.
    /// </summary>
    /// <param name="imageId">The image identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the image exists; false otherwise.</returns>
    Task<bool> ExistsAsync(Guid imageId, CancellationToken ct);
}
