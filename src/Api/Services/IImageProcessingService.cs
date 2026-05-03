namespace LocationManagement.Api.Services;

/// <summary>
/// Represents the set of pre-generated image variants for a single uploaded image.
/// </summary>
public sealed record ImageVariants(
    Guid ImageId,
    string ThumbnailUrl,
    string Variant400Url,
    string Variant700Url,
    string Variant1000Url
);

/// <summary>
/// Processes and stores images, generating thumbnail and responsive variants.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Processes an uploaded image, generates variants, and stores them atomically.
    /// </summary>
    /// <param name="imageStream">The image file stream.</param>
    /// <param name="mimeType">The MIME type of the image (must be image/jpeg, image/png, or image/webp).</param>
    /// <param name="altText">Optional alt text for the image (max 500 characters).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Image metadata including all variant URLs.</returns>
    /// <exception cref="ArgumentException">Thrown if MIME type is not supported or altText exceeds 500 characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown if file size exceeds 10 MB or variant generation fails.</exception>
    Task<ImageVariants> ProcessAndStoreAsync(Stream imageStream, string mimeType, string? altText, CancellationToken ct);

    /// <summary>
    /// Deletes an image and all its variants from storage and the database.
    /// </summary>
    /// <param name="imageId">The image identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteImageAndVariantsAsync(Guid imageId, CancellationToken ct);
}
