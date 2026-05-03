using System.ComponentModel.DataAnnotations;

namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Request DTO for creating a new Location.
/// </summary>
public sealed class CreateLocationRequest
{
    /// <summary>
    /// Gets or sets the Location name.
    /// </summary>
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200, ErrorMessage = "Name must not exceed 200 characters.")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the latitude coordinate.
    /// </summary>
    [Required(ErrorMessage = "Latitude is required.")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public required double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude coordinate.
    /// </summary>
    [Required(ErrorMessage = "Longitude is required.")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public required double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the Spatial Reference ID (SRID). Defaults to 4326 (WGS84) if not provided.
    /// </summary>
    public int SourceSrid { get; set; } = 4326;

    /// <summary>
    /// Gets or sets the ContentSequence as a JSON string.
    /// </summary>
    [Required(ErrorMessage = "ContentSequence is required.")]
    public required string ContentSequence { get; set; }
}
