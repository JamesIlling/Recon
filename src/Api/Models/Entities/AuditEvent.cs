using LocationManagement.Api.Models.Enums;

namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents a tamper-evident, append-only record of a significant system action.
/// AuditEvents are immutable after creation and retained for a minimum of 1 year.
/// </summary>
public class AuditEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit event.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the type of event (e.g., LocationCreated, LocationEdited, UserRegistered).
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who performed the action.
    /// Nullable for unauthenticated or system-initiated actions.
    /// Foreign key to User.Id.
    /// </summary>
    public Guid? ActingUserId { get; set; }

    /// <summary>
    /// Gets or sets the type of resource affected by the action (e.g., Location, User, Collection).
    /// Nullable for events that do not target a specific resource type.
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the resource affected by the action.
    /// Nullable for events that do not target a specific resource.
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the outcome of the action (Success or Failure).
    /// </summary>
    public required AuditOutcome Outcome { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the event occurred.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the navigation property for the user who performed the action.
    /// </summary>
    public virtual User? ActingUser { get; set; }
}
