using LocationManagement.Api.Models.Enums;

namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents a pending request to add a Location to a LocationCollection,
/// awaiting approval or rejection by the collection owner.
/// </summary>
public class PendingMembershipRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the pending membership request.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the collection for which membership is requested.
    /// Foreign key to LocationCollection.Id.
    /// </summary>
    public required Guid CollectionId { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the user who requested membership.
    /// Foreign key to User.Id.
    /// </summary>
    public required Guid RequestedByUserId { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the location being requested for membership.
    /// Foreign key to Location.Id.
    /// </summary>
    public required Guid LocationId { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the membership request was submitted.
    /// </summary>
    public required DateTimeOffset RequestedAt { get; init; }

    /// <summary>
    /// Gets or sets the current status of the membership request (Pending, Approved, or Rejected).
    /// </summary>
    public required MembershipRequestStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the collection.
    /// </summary>
    public virtual LocationCollection Collection { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property for the user who requested membership.
    /// </summary>
    public virtual User RequestedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property for the location being requested.
    /// </summary>
    public virtual Location Location { get; set; } = null!;
}
