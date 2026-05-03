using Microsoft.EntityFrameworkCore;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository implementation for NamedShape entity CRUD operations.
/// </summary>
public sealed class NamedShapeRepository : INamedShapeRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedShapeRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public NamedShapeRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a new NamedShape in the database.
    /// </summary>
    /// <param name="namedShape">The NamedShape entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created NamedShape with its ID populated.</returns>
    public async Task<NamedShape> CreateAsync(NamedShape namedShape, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(namedShape);

        _context.NamedShapes.Add(namedShape);
        await _context.SaveChangesAsync(cancellationToken);

        return namedShape;
    }

    /// <summary>
    /// Retrieves a NamedShape by its ID.
    /// </summary>
    /// <param name="id">The NamedShape ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The NamedShape if found; otherwise null.</returns>
    public async Task<NamedShape?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.NamedShapes
            .Include(ns => ns.CreatedByUser)
            .FirstOrDefaultAsync(ns => ns.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves a NamedShape by its name (case-insensitive).
    /// </summary>
    /// <param name="name">The NamedShape name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The NamedShape if found; otherwise null.</returns>
    public async Task<NamedShape?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await _context.NamedShapes
            .Include(ns => ns.CreatedByUser)
            .FirstOrDefaultAsync(ns => ns.Name.ToLower() == name.ToLower(), cancellationToken);
    }

    /// <summary>
    /// Retrieves a paginated list of NamedShapes in descending order of creation timestamp.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the total count and the list of NamedShapes for the page.</returns>
    public async Task<(int TotalCount, List<NamedShape> Items)> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1)
            throw new ArgumentException("Page must be >= 1.", nameof(page));
        if (pageSize < 1)
            throw new ArgumentException("PageSize must be >= 1.", nameof(pageSize));

        var query = _context.NamedShapes
            .Include(ns => ns.CreatedByUser)
            .OrderByDescending(ns => ns.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (totalCount, items);
    }

    /// <summary>
    /// Updates an existing NamedShape.
    /// </summary>
    /// <param name="namedShape">The NamedShape entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated NamedShape.</returns>
    public async Task<NamedShape> UpdateAsync(NamedShape namedShape, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(namedShape);

        _context.NamedShapes.Update(namedShape);
        await _context.SaveChangesAsync(cancellationToken);

        return namedShape;
    }

    /// <summary>
    /// Deletes a NamedShape by its ID.
    /// </summary>
    /// <param name="id">The NamedShape ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the NamedShape was deleted; false if not found.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var namedShape = await _context.NamedShapes.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        if (namedShape == null)
            return false;

        _context.NamedShapes.Remove(namedShape);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Checks if a NamedShape is referenced by any LocationCollection.
    /// </summary>
    /// <param name="id">The NamedShape ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the NamedShape is referenced; otherwise false.</returns>
    public async Task<bool> IsReferencedByCollectionAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.LocationCollections
            .AnyAsync(lc => lc.BoundingShapeId == id, cancellationToken);
    }
}
