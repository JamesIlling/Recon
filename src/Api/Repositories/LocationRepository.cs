using Microsoft.EntityFrameworkCore;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository implementation for Location entity CRUD operations and spatial queries.
/// </summary>
public sealed class LocationRepository : ILocationRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public LocationRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a new Location in the database.
    /// </summary>
    /// <param name="location">The Location entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created Location with its ID populated.</returns>
    public async Task<Location> CreateAsync(Location location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);

        _context.Locations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        return location;
    }

    /// <summary>
    /// Retrieves a Location by its ID.
    /// </summary>
    /// <param name="id">The Location ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Location if found; otherwise null.</returns>
    public async Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Locations
            .Include(l => l.Creator)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves a paginated list of Locations in descending order of creation timestamp.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the total count and the list of Locations for the page.</returns>
    public async Task<(int TotalCount, List<Location> Items)> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1)
            throw new ArgumentException("Page must be >= 1.", nameof(page));
        if (pageSize < 1)
            throw new ArgumentException("PageSize must be >= 1.", nameof(pageSize));

        var query = _context.Locations
            .Include(l => l.Creator)
            .OrderByDescending(l => l.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (totalCount, items);
    }

    /// <summary>
    /// Updates an existing Location.
    /// </summary>
    /// <param name="location">The Location entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated Location.</returns>
    public async Task<Location> UpdateAsync(Location location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);

        _context.Locations.Update(location);
        await _context.SaveChangesAsync(cancellationToken);

        return location;
    }

    /// <summary>
    /// Deletes a Location by its ID.
    /// </summary>
    /// <param name="id">The Location ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the Location was deleted; false if not found.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var location = await _context.Locations.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        if (location == null)
            return false;

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Retrieves all Locations created by a specific user.
    /// </summary>
    /// <param name="creatorId">The creator's User ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of Locations created by the user.</returns>
    public async Task<List<Location>> GetByCreatorIdAsync(Guid creatorId, CancellationToken cancellationToken)
    {
        return await _context.Locations
            .Where(l => l.CreatorId == creatorId)
            .Include(l => l.Creator)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a Location exists by its ID.
    /// </summary>
    /// <param name="id">The Location ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the Location exists; otherwise false.</returns>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Locations.AnyAsync(l => l.Id == id, cancellationToken);
    }
}
