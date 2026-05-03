namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents the association between a Location and a LocationCollection.
/// Uses a composite primary key of (LocationId, CollectionId).
/// </summary>
public class CollectionMember
{
    /// <summary>
    /// Gets or sets the identifier of the location in this membership.
    /// Part of the composite primary key. Foreign key to Location.Id.
    /// </summary>
    public required Guid LocationId { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the collection in this membership.
    /// Part of the composite primary key. Foreign key to LocationCollection.Id.
    /// </summary>
    public required Guid CollectionId { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the location was added to the collection.
    /// </summary>
    public required DateTimeOffset AddedAt { get; init; }

    /// <summary>
    /// Gets or sets the navigation property for the location in this membership.
    /// </summary>
    public virtual Location Location { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property for the collection in this membership.
    /// </summary>
    public virtual LocationCollection Collection { get; set; } = null!;
}
