namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents a named collection of Locations, optionally public, with optional metadata
/// including a bounding shape and collection image.
/// </summary>
public class LocationCollection
{
    /// <summary>
    /// Gets or sets the unique identifier for the collection.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the name of the collection.
    /// Maximum length: 200 characters.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional description of the collection.
    /// Maximum length: 1000 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who owns this collection.
    /// Foreign key to User.Id.
    /// </summary>
    public required Guid OwnerId { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the optional thumbnail image for the collection.
    /// Foreign key to Image.Id. Nullable.
    /// </summary>
    public Guid? ThumbnailImageId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the optional bounding shape for the collection.
    /// Foreign key to NamedShape.Id. Nullable.
    /// </summary>
    public Guid? BoundingShapeId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this collection is public (visible to all users)
    /// or private (visible only to the owner).
    /// Defaults to false (private).
    /// </summary>
    public required bool IsPublic { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the collection was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the collection was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the user who owns this collection.
    /// </summary>
    public virtual User Owner { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property for the optional thumbnail image.
    /// </summary>
    public virtual Image? ThumbnailImage { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the optional bounding shape.
    /// </summary>
    public virtual NamedShape? BoundingShape { get; set; }

    /// <summary>
    /// Gets or sets the collection of member locations in this collection.
    /// </summary>
    public virtual ICollection<CollectionMember> Members { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of pending membership requests for this collection.
    /// </summary>
    public virtual ICollection<PendingMembershipRequest> PendingMembershipRequests { get; set; } = [];
}
