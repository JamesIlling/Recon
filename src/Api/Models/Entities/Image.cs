namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents an uploaded image with pre-generated variants (thumbnail and responsive sizes).
/// </summary>
public class Image
{
    /// <summary>
    /// Gets or sets the unique identifier for the image.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the original filename of the uploaded image.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the image (e.g., image/jpeg, image/png, image/webp).
    /// </summary>
    public required string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the optional alt text for the image.
    /// Maximum length: 500 characters.
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public required long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the URL to the original full-resolution image.
    /// </summary>
    public required string OriginalUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL to the thumbnail variant (200x200 square crop).
    /// </summary>
    public required string ThumbnailUrl { get; set; }

    /// <summary>
    /// Gets or sets the responsive variant URLs as a JSON-serialized array.
    /// Contains URLs for 400px, 700px, and 1000px wide variants.
    /// </summary>
    public required string ResponsiveVariantUrls { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who uploaded this image.
    /// Foreign key to User.Id.
    /// </summary>
    public required Guid UploadedByUserId { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the image was uploaded.
    /// </summary>
    public required DateTimeOffset UploadedAt { get; init; }

    /// <summary>
    /// Gets or sets the navigation property for the user who uploaded this image.
    /// </summary>
    public virtual User UploadedByUser { get; set; } = null!;
}
