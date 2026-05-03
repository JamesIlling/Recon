using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;

namespace LocationManagement.Api.Services;

/// <summary>
/// DTO for user admin list response.
/// </summary>
public sealed record UserAdminDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Email,
    UserRole Role,
    DateTimeOffset CreatedAt
);

/// <summary>
/// Service interface for admin user management operations.
/// </summary>
public interface IUserAdminService
{
    /// <summary>
    /// Lists all users with pagination, ordered by creation date descending.
    /// </summary>
    /// <param name="page">The page number (1-indexed).</param>
    /// <param name="pageSize">The number of users per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the list of users and the total count.</returns>
    Task<(List<UserAdminDto> Users, int TotalCount)> ListUsersAsync(int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Promotes a Standard user to Admin role.
    /// Records an AuditEvent for the promotion.
    /// </summary>
    /// <param name="userId">The ID of the user to promote.</param>
    /// <param name="actingUserId">The ID of the admin performing the promotion.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user DTO.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the user is already an Admin.</exception>
    Task<UserAdminDto> PromoteAsync(Guid userId, Guid actingUserId, CancellationToken ct);

    /// <summary>
    /// Demotes an Admin user to Standard role.
    /// Prevents demotion of the last admin in the system.
    /// Records an AuditEvent for the demotion.
    /// </summary>
    /// <param name="userId">The ID of the user to demote.</param>
    /// <param name="actingUserId">The ID of the admin performing the demotion.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user DTO.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the user is already Standard, or if this is the last admin.</exception>
    Task<UserAdminDto> DemoteAsync(Guid userId, Guid actingUserId, CancellationToken ct);

    /// <summary>
    /// Reassigns ownership of a resource (Location or LocationCollection) to a new owner.
    /// Records an AuditEvent for the reassignment.
    /// </summary>
    /// <param name="resourceType">The type of resource: "Location" or "LocationCollection".</param>
    /// <param name="resourceId">The ID of the resource to reassign.</param>
    /// <param name="newOwnerId">The ID of the new owner user.</param>
    /// <param name="actingUserId">The ID of the admin performing the reassignment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A DTO representing the updated resource.</returns>
    /// <exception cref="ArgumentException">Thrown if resourceType is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the resource or new owner is not found.</exception>
    Task<object> ReassignResourceOwnershipAsync(string resourceType, Guid resourceId, Guid newOwnerId, Guid actingUserId, CancellationToken ct);
}
