using Microsoft.EntityFrameworkCore;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository implementation for User entity CRUD operations.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UserRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Retrieves a User by their ID.
    /// </summary>
    /// <param name="id">The User ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The User if found; otherwise null.</returns>
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Include(u => u.AvatarImage)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves a User by their username (case-insensitive).
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The User if found; otherwise null.</returns>
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return await _context.Users
            .Include(u => u.AvatarImage)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);
    }

    /// <summary>
    /// Retrieves a User by their display name (case-insensitive).
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The User if found; otherwise null.</returns>
    public async Task<User?> GetByDisplayNameAsync(string displayName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return await _context.Users
            .Include(u => u.AvatarImage)
            .FirstOrDefaultAsync(u => u.DisplayName.ToLower() == displayName.ToLower(), cancellationToken);
    }

    /// <summary>
    /// Retrieves a User by their email address (case-insensitive).
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The User if found; otherwise null.</returns>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return await _context.Users
            .Include(u => u.AvatarImage)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    /// <summary>
    /// Updates an existing User.
    /// </summary>
    /// <param name="user">The User entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated User.</returns>
    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.UpdatedAt = DateTimeOffset.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    /// <summary>
    /// Checks if a display name is already in use by another user (case-insensitive).
    /// </summary>
    /// <param name="displayName">The display name to check.</param>
    /// <param name="excludeUserId">The User ID to exclude from the check (typically the current user).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the display name is in use by another user; otherwise false.</returns>
    public async Task<bool> IsDisplayNameInUseAsync(string displayName, Guid excludeUserId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return await _context.Users
            .AnyAsync(u => u.DisplayName.ToLower() == displayName.ToLower() && u.Id != excludeUserId, cancellationToken);
    }

    /// <summary>
    /// Lists all users with pagination, ordered by creation date descending.
    /// </summary>
    /// <param name="skip">The number of users to skip (for pagination).</param>
    /// <param name="take">The number of users to take (page size).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the list of users and the total count.</returns>
    public async Task<(List<User> Users, int TotalCount)> ListUsersAsync(int skip, int take, CancellationToken cancellationToken)
    {
        if (skip < 0)
        {
            throw new ArgumentException("Skip must be 0 or greater.", nameof(skip));
        }

        if (take < 1)
        {
            throw new ArgumentException("Take must be 1 or greater.", nameof(take));
        }

        var totalCount = await _context.Users.CountAsync(cancellationToken);
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    /// <summary>
    /// Counts the number of users with the Admin role.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The count of admin users.</returns>
    public async Task<int> CountAdminsAsync(CancellationToken cancellationToken)
    {
        return await _context.Users
            .CountAsync(u => u.Role == LocationManagement.Api.Models.Enums.UserRole.Admin, cancellationToken);
    }
}
