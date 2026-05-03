using Microsoft.EntityFrameworkCore;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository implementation for PendingEdit entity operations.
/// </summary>
public sealed class PendingEditRepository : IPendingEditRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PendingEditRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PendingEditRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a new PendingEdit in the database.
    /// </summary>
    /// <param name="pendingEdit">The PendingEdit entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created PendingEdit.</returns>
    public async Task<PendingEdit> CreateAsync(PendingEdit pendingEdit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingEdit);

        _context.PendingEdits.Add(pendingEdit);
        await _context.SaveChangesAsync(cancellationToken);

        return pendingEdit;
    }

    /// <summary>
    /// Retrieves a PendingEdit by its ID.
    /// </summary>
    /// <param name="id">The PendingEdit ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PendingEdit if found; otherwise null.</returns>
    public async Task<PendingEdit?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.PendingEdits
            .Include(pe => pe.SubmittedByUser)
            .Include(pe => pe.Location)
            .FirstOrDefaultAsync(pe => pe.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves all PendingEdits for a specific Location.
    /// </summary>
    /// <param name="locationId">The Location ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of PendingEdits for the Location.</returns>
    public async Task<List<PendingEdit>> GetByLocationIdAsync(Guid locationId, CancellationToken cancellationToken)
    {
        return await _context.PendingEdits
            .Where(pe => pe.LocationId == locationId)
            .Include(pe => pe.SubmittedByUser)
            .OrderByDescending(pe => pe.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a PendingEdit for a specific Location and submitting User.
    /// </summary>
    /// <param name="locationId">The Location ID.</param>
    /// <param name="submittedByUserId">The User ID who submitted the edit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PendingEdit if found; otherwise null.</returns>
    public async Task<PendingEdit?> GetByLocationAndUserAsync(Guid locationId, Guid submittedByUserId, CancellationToken cancellationToken)
    {
        return await _context.PendingEdits
            .Include(pe => pe.SubmittedByUser)
            .Include(pe => pe.Location)
            .FirstOrDefaultAsync(pe => pe.LocationId == locationId && pe.SubmittedByUserId == submittedByUserId, cancellationToken);
    }

    /// <summary>
    /// Updates an existing PendingEdit.
    /// </summary>
    /// <param name="pendingEdit">The PendingEdit entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated PendingEdit.</returns>
    public async Task<PendingEdit> UpdateAsync(PendingEdit pendingEdit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingEdit);

        _context.PendingEdits.Update(pendingEdit);
        await _context.SaveChangesAsync(cancellationToken);

        return pendingEdit;
    }

    /// <summary>
    /// Deletes a PendingEdit by its ID.
    /// </summary>
    /// <param name="id">The PendingEdit ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the PendingEdit was deleted; false if not found.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var pendingEdit = await _context.PendingEdits.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        if (pendingEdit == null)
            return false;

        _context.PendingEdits.Remove(pendingEdit);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Deletes all PendingEdits for a specific Location.
    /// </summary>
    /// <param name="locationId">The Location ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of PendingEdits deleted.</returns>
    public async Task<int> DeleteByLocationIdAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var pendingEdits = await _context.PendingEdits
            .Where(pe => pe.LocationId == locationId)
            .ToListAsync(cancellationToken);

        _context.PendingEdits.RemoveRange(pendingEdits);
        await _context.SaveChangesAsync(cancellationToken);

        return pendingEdits.Count;
    }

    /// <summary>
    /// Checks if a PendingEdit exists by its ID.
    /// </summary>
    /// <param name="id">The PendingEdit ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the PendingEdit exists; otherwise false.</returns>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.PendingEdits.AnyAsync(pe => pe.Id == id, cancellationToken);
    }
}
