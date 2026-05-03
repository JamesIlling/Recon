using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository implementation for LocationCollection entity CRUD operations.
/// </summary>
public class CollectionRepository : ICollectionRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public CollectionRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<LocationCollection> CreateAsync(LocationCollection collection, CancellationToken cancellationToken)
    {
        _context.LocationCollections.Add(collection);
        await _context.SaveChangesAsync(cancellationToken);
        return collection;
    }

    /// <inheritdoc />
    public async Task<LocationCollection?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.LocationCollections
            .Include(c => c.Owner)
            .Include(c => c.ThumbnailImage)
            .Include(c => c.BoundingShape)
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(int TotalCount, List<LocationCollection> Items)> ListPublicAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.LocationCollections
            .Where(c => c.IsPublic)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Owner)
            .Include(c => c.ThumbnailImage)
            .ToListAsync(cancellationToken);

        return (totalCount, items);
    }

    /// <inheritdoc />
    public async Task<(int TotalCount, List<LocationCollection> Items)> ListCombinedAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.LocationCollections
            .Where(c => c.IsPublic || c.OwnerId == userId)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Owner)
            .Include(c => c.ThumbnailImage)
            .ToListAsync(cancellationToken);

        return (totalCount, items);
    }

    /// <inheritdoc />
    public async Task<LocationCollection> UpdateAsync(LocationCollection collection, CancellationToken cancellationToken)
    {
        _context.LocationCollections.Update(collection);
        await _context.SaveChangesAsync(cancellationToken);
        return collection;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var collection = await _context.LocationCollections.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        if (collection == null)
        {
            return false;
        }

        _context.LocationCollections.Remove(collection);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<LocationCollection>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return await _context.LocationCollections
            .Where(c => c.OwnerId == ownerId)
            .OrderByDescending(c => c.CreatedAt)
            .Include(c => c.Owner)
            .Include(c => c.ThumbnailImage)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.LocationCollections.AnyAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsNamedShapeReferencedAsync(Guid namedShapeId, CancellationToken cancellationToken)
    {
        return await _context.LocationCollections
            .AnyAsync(c => c.BoundingShapeId == namedShapeId, cancellationToken);
    }
}
