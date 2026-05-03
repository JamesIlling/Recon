using NetTopologySuite.Geometries;

namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents a proposed set of changes to a Location submitted by a non-creator user,
/// awaiting approval or rejection by the Location's creator.
/// </summary>
public class PendingEdit
{
    /// <summary>
    /// Gets or sets the unique identifier for the pending edit.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the location being edited.
    /// Foreign key to Location.Id.
    /// </summary>
    public required Guid LocationId { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the user who submitted this pending edit.
    /// Foreign key to User.Id.
    /// </summary>
    public required Guid SubmittedByUserId { get; init; }

    /// <summary>
    /// Gets or sets the proposed name for the location.
    /// Maximum length: 200 characters.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the proposed latitude component.
    /// </summary>
    public required double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the proposed longitude component.
    /// </summary>
    public required double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the proposed geographic coordinates as a NetTopologySuite Point.
    /// Always stored in WGS84 (EPSG:4326) after reprojection from the source SRID.
    /// Rounded to 6 decimal places.
    /// </summary>
    public required Point Coordinates { get; set; }

    /// <summary>
    /// Gets or sets the Spatial Reference ID of the source coordinate system
    /// from which the proposed coordinates were reprojected to WGS84.
    /// </summary>
    public required int SourceSrid { get; set; }

    /// <summary>
    /// Gets or sets the proposed content sequence as a JSON-serialized string.
    /// Contains an ordered array of ContentBlock objects.
    /// </summary>
    public required string ContentSequence { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the pending edit was submitted.
    /// </summary>
    public required DateTimeOffset SubmittedAt { get; init; }

    /// <summary>
    /// Gets or sets the navigation property for the location being edited.
    /// </summary>
    public virtual Location Location { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property for the user who submitted this edit.
    /// </summary>
    public virtual User SubmittedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of images associated with this pending edit's content.
    /// </summary>
    public virtual ICollection<Image> Images { get; set; } = [];
}
