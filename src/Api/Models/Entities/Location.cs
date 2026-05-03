using NetTopologySuite.Geometries;

namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents a geographic location with coordinates, spatial reference information, and formatted content.
/// </summary>
public class Location
{
    /// <summary>
    /// Gets or sets the unique identifier for the location.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the name of the location.
    /// Maximum length: 200 characters.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the latitude component of the location (derived from Coordinates).
    /// Stored for convenience; the authoritative value is in Coordinates.
    /// </summary>
    public required double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude component of the location (derived from Coordinates).
    /// Stored for convenience; the authoritative value is in Coordinates.
    /// </summary>
    public required double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the geographic coordinates as a NetTopologySuite Point.
    /// Always stored in WGS84 (EPSG:4326) after reprojection from the source SRID.
    /// Rounded to 6 decimal places (approximately 11.1 cm ground accuracy).
    /// </summary>
    public required Point Coordinates { get; set; }

    /// <summary>
    /// Gets or sets the Spatial Reference ID (SRID) of the source coordinate system
    /// from which the coordinates were reprojected to WGS84.
    /// Retained as metadata for reference.
    /// </summary>
    public required int SourceSrid { get; set; }

    /// <summary>
    /// Gets or sets the content sequence as a JSON-serialized string.
    /// Contains an ordered array of ContentBlock objects (Heading, Paragraph, Image).
    /// </summary>
    public required string ContentSequence { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created this location.
    /// Foreign key to User.Id.
    /// </summary>
    public required Guid CreatorId { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the location was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the location was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the user who created this location.
    /// </summary>
    public virtual User Creator { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of pending edits for this location.
    /// </summary>
    public virtual ICollection<PendingEdit> PendingEdits { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of collection memberships for this location.
    /// </summary>
    public virtual ICollection<CollectionMember> CollectionMembers { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of images associated with this location's content.
    /// </summary>
    public virtual ICollection<Image> Images { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of pending membership requests for this location.
    /// </summary>
    public virtual ICollection<PendingMembershipRequest> PendingMembershipRequests { get; set; } = [];
}
