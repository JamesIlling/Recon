using LocationManagement.Api.Models.Enums;

namespace LocationManagement.Api.Services;

/// <summary>
/// DTO for audit event response.
/// </summary>
public sealed record AuditEventDto(
    Guid Id,
    string EventType,
    Guid? ActingUserId,
    string? ResourceType,
    Guid? ResourceId,
    AuditOutcome Outcome,
    DateTimeOffset CreatedAt
);

/// <summary>
/// Records append-only audit events for all significant system operations.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Records an audit event for a system operation.
    /// </summary>
    /// <param name="eventType">The type of event (e.g., "LocationCreated", "UserRegistered").</param>
    /// <param name="actingUserId">The ID of the user performing the action, or null for anonymous actions.</param>
    /// <param name="targetResourceType">The type of resource affected (e.g., "Location", "User"), or null if not applicable.</param>
    /// <param name="targetResourceId">The ID of the resource affected, or null if not applicable.</param>
    /// <param name="outcome">Whether the operation succeeded or failed.</param>
    /// <param name="sourceIp">The IP address from which the request originated.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordAsync(
        string eventType,
        Guid? actingUserId,
        string? targetResourceType,
        Guid? targetResourceId,
        AuditOutcome outcome,
        string sourceIp,
        CancellationToken ct);

    /// <summary>
    /// Retrieves audit events with optional filtering and pagination.
    /// </summary>
    /// <param name="eventType">Filter by event type (optional).</param>
    /// <param name="actingUserId">Filter by the user who performed the action (optional).</param>
    /// <param name="resourceType">Filter by resource type (optional).</param>
    /// <param name="resourceId">Filter by specific resource ID (optional).</param>
    /// <param name="outcome">Filter by outcome (optional).</param>
    /// <param name="startDate">Filter by start date (optional).</param>
    /// <param name="endDate">Filter by end date (optional).</param>
    /// <param name="page">The page number (1-indexed).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the list of audit events and the total count.</returns>
    Task<(List<AuditEventDto> AuditEvents, int TotalCount)> GetAuditLogAsync(
        string? eventType,
        Guid? actingUserId,
        string? resourceType,
        Guid? resourceId,
        AuditOutcome? outcome,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        int page,
        int pageSize,
        CancellationToken ct);
}
