using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Repositories;

/// <summary>
/// Repository interface for User entity CRUD operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a User by their ID.
    /// </summary>
    /// <param name="id">The User ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The User if found; otherwise null.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a User by their username (case-insensitive).
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The User if found; otherwise null.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a User by their display name (case-insensitive).
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The User if found; otherwise null.</returns>
    Task<User?> GetByDisplayNameAsync(string displayName, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a User by their email address (case-insensitive).
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The User if found; otherwise null.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing User.
    /// </summary>
    /// <param name="user">The User entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated User.</returns>
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a display name is already in use by another user (case-insensitive).
    /// </summary>
    /// <param name="displayName">The display name to check.</param>
    /// <param name="excludeUserId">The User ID to exclude from the check (typically the current user).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the display name is in use by another user; otherwise false.</returns>
    Task<bool> IsDisplayNameInUseAsync(string displayName, Guid excludeUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all users with pagination, ordered by creation date descending.
    /// </summary>
    /// <param name="skip">The number of users to skip (for pagination).</param>
    /// <param name="take">The number of users to take (page size).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the list of users and the total count.</returns>
    Task<(List<User> Users, int TotalCount)> ListUsersAsync(int skip, int take, CancellationToken cancellationToken);

    /// <summary>
    /// Counts the number of users with the Admin role.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The count of admin users.</returns>
    Task<int> CountAdminsAsync(CancellationToken cancellationToken);
}
