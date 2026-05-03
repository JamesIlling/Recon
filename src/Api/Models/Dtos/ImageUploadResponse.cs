namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Response DTO for image upload, containing all variant URLs.
/// </summary>
public class ImageUploadResponse
{
    /// <summary>
    /// Gets or sets the unique image identifier.
    /// </summary>
    public required Guid ImageId { get; set; }

    /// <summary>
    /// Gets or sets the URL to the full-resolution original image.
    /// </summary>
    public required string OriginalUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL to the thumbnail variant (200x200 square crop).
    /// </summary>
    public required string ThumbnailUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL to the 400px responsive variant.
    /// </summary>
    public required string Variant400Url { get; set; }

    /// <summary>
    /// Gets or sets the URL to the 700px responsive variant.
    /// </summary>
    public required string Variant700Url { get; set; }

    /// <summary>
    /// Gets or sets the URL to the 1000px responsive variant.
    /// </summary>
    public required string Variant1000Url { get; set; }

    /// <summary>
    /// Gets or sets the optional alt text for the image.
    /// </summary>
    public string? AltText { get; set; }
}
