using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocationManagement.Api.Services;

/// <summary>
/// Records append-only audit events to the database.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditService"/> class.
    /// </summary>
    public AuditService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Records an audit event for a system operation.
    /// </summary>
    public async Task RecordAsync(
        string eventType,
        Guid? actingUserId,
        string? targetResourceType,
        Guid? targetResourceId,
        AuditOutcome outcome,
        string sourceIp,
        CancellationToken ct)
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            ActingUserId = actingUserId,
            ResourceType = targetResourceType,
            ResourceId = targetResourceId,
            Outcome = outcome,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.AuditEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Retrieves audit events with optional filtering and pagination.
    /// </summary>
    public async Task<(List<AuditEventDto> AuditEvents, int TotalCount)> GetAuditLogAsync(
        string? eventType,
        Guid? actingUserId,
        string? resourceType,
        Guid? resourceId,
        AuditOutcome? outcome,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (page < 1)
        {
            throw new ArgumentException("Page must be 1 or greater.", nameof(page));
        }

        if (pageSize < 1 || pageSize > 200)
        {
            throw new ArgumentException("Page size must be between 1 and 200.", nameof(pageSize));
        }

        var query = _dbContext.AuditEvents.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(ae => ae.EventType == eventType);
        }

        if (actingUserId.HasValue)
        {
            query = query.Where(ae => ae.ActingUserId == actingUserId);
        }

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            query = query.Where(ae => ae.ResourceType == resourceType);
        }

        if (resourceId.HasValue)
        {
            query = query.Where(ae => ae.ResourceId == resourceId);
        }

        if (outcome.HasValue)
        {
            query = query.Where(ae => ae.Outcome == outcome);
        }

        if (startDate.HasValue)
        {
            query = query.Where(ae => ae.CreatedAt >= startDate);
        }

        if (endDate.HasValue)
        {
            query = query.Where(ae => ae.CreatedAt <= endDate);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply pagination and ordering
        var skip = (page - 1) * pageSize;
        var auditEvents = await query
            .OrderByDescending(ae => ae.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(ae => new AuditEventDto(
                ae.Id,
                ae.EventType,
                ae.ActingUserId,
                ae.ResourceType,
                ae.ResourceId,
                ae.Outcome,
                ae.CreatedAt))
            .ToListAsync(ct);

        return (auditEvents, totalCount);
    }
}
