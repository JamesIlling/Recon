using Microsoft.EntityFrameworkCore;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository implementation for Image entity CRUD operations.
/// </summary>
public sealed class ImageRepository : IImageRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ImageRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a new image record in the database.
    /// </summary>
    /// <param name="image">The image entity to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created image entity.</returns>
    public async Task<Image> CreateAsync(Image image, CancellationToken ct)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        _context.Images.Add(image);
        await _context.SaveChangesAsync(ct);
        return image;
    }

    /// <summary>
    /// Retrieves an image by its identifier.
    /// </summary>
    /// <param name="imageId">The image identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The image entity, or null if not found.</returns>
    public async Task<Image?> GetByIdAsync(Guid imageId, CancellationToken ct)
    {
        return await _context.Images
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == imageId, ct);
    }

    /// <summary>
    /// Retrieves all images uploaded by a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of image entities.</returns>
    public async Task<IEnumerable<Image>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Images
            .AsNoTracking()
            .Where(i => i.UploadedByUserId == userId)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Deletes an image record from the database.
    /// </summary>
    /// <param name="imageId">The image identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the image was deleted; false if not found.</returns>
    public async Task<bool> DeleteAsync(Guid imageId, CancellationToken ct)
    {
        var image = await _context.Images.FirstOrDefaultAsync(i => i.Id == imageId, ct);
        if (image == null)
        {
            return false;
        }

        _context.Images.Remove(image);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Updates an existing image record.
    /// </summary>
    /// <param name="image">The image entity with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated image entity.</returns>
    public async Task<Image> UpdateAsync(Image image, CancellationToken ct)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        _context.Images.Update(image);
        await _context.SaveChangesAsync(ct);
        return image;
    }

    /// <summary>
    /// Checks if an image with the given identifier exists.
    /// </summary>
    /// <param name="imageId">The image identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the image exists; false otherwise.</returns>
    public async Task<bool> ExistsAsync(Guid imageId, CancellationToken ct)
    {
        return await _context.Images.AnyAsync(i => i.Id == imageId, ct);
    }
}
