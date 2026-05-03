using NetTopologySuite.Geometries;

namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents a named geographic shape (polygon or multi-polygon) that can be used
/// as a bounding shape for LocationCollections.
/// </summary>
public class NamedShape
{
    /// <summary>
    /// Gets or sets the unique identifier for the named shape.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the name of the shape.
    /// Maximum length: 200 characters. Must be unique (case-insensitive).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the geographic geometry as a NetTopologySuite Geometry object.
    /// Typically a Polygon or MultiPolygon. Stored as GEOGRAPHY type in the database.
    /// </summary>
    public required Geometry Geometry { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the shape was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the user (admin) who created this shape.
    /// Foreign key to User.Id.
    /// </summary>
    public required Guid CreatedByUserId { get; init; }

    /// <summary>
    /// Gets or sets the navigation property for the user who created this shape.
    /// </summary>
    public virtual User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of location collections that use this shape as their bounding shape.
    /// </summary>
    public virtual ICollection<LocationCollection> Collections { get; set; } = [];
}
