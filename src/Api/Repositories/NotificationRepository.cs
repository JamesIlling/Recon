using Microsoft.EntityFrameworkCore;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository implementation for Notification entity operations.
/// </summary>
public sealed class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public NotificationRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a new Notification in the database.
    /// </summary>
    public async Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return notification;
    }

    /// <summary>
    /// Retrieves a Notification by its ID.
    /// </summary>
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves all Notifications for a specific user.
    /// </summary>
    public async Task<List<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .Include(n => n.User)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a Notification by its ID.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        if (notification == null)
            return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Checks if a Notification exists by its ID.
    /// </summary>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Notifications.AnyAsync(n => n.Id == id, cancellationToken);
    }

    /// <summary>
    /// Updates an existing Notification in the database.
    /// </summary>
    public async Task<Notification> UpdateAsync(Notification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return notification;
    }
}
