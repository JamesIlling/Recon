using System.Security.Claims;
using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Repositories;
using LocationManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocationManagement.Api.Controllers;

/// <summary>
/// Provides image upload and serving endpoints with support for thumbnails and responsive variants.
/// </summary>
[ApiController]
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private readonly IImageRepository _imageRepository;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILocalFileStorageService _fileStorageService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        IImageRepository imageRepository,
        IImageProcessingService imageProcessingService,
        ILocalFileStorageService fileStorageService,
        ILogger<ImagesController> logger)
    {
        _imageRepository = imageRepository ?? throw new ArgumentNullException(nameof(imageRepository));
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Uploads an image, generates variants, and returns all variant URLs.
    /// </summary>
    /// <param name="file">The image file to upload.</param>
    /// <param name="altText">Optional alt text for the image (max 500 characters).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Image metadata including all variant URLs.</returns>
    /// <response code="201">Image successfully uploaded and processed.</response>
    /// <response code="400">Validation error (invalid alt text length, etc.).</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="413">File size exceeds 10 MB limit.</response>
    /// <response code="415">Unsupported MIME type (only JPEG, PNG, WebP allowed).</response>
    /// <response code="422">Image processing failed (corrupt image, etc.).</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string? altText,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required." });
        }

        // Validate alt text length
        if (!string.IsNullOrEmpty(altText) && altText.Length > 500)
        {
            return BadRequest(new { error = "Alt text must not exceed 500 characters." });
        }

        // Validate MIME type
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedMimeTypes.Contains(file.ContentType))
        {
            _logger.LogWarning("Upload rejected: unsupported MIME type {MimeType}", file.ContentType);
            return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                new { error = "Only JPEG, PNG, and WebP images are supported." });
        }

        // Validate file size (10 MB)
        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            _logger.LogWarning("Upload rejected: file size {Size} exceeds limit", file.Length);
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new { error = "File size must not exceed 10 MB." });
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return Unauthorized(new { error = "Invalid user context." });
            }

            // Process and store the image
            using var stream = file.OpenReadStream();
            var variants = await _imageProcessingService.ProcessAndStoreAsync(
                stream,
                file.ContentType,
                altText,
                ct);

            // Create image record in database
            var image = new Models.Entities.Image
            {
                Id = variants.ImageId,
                FileName = file.FileName,
                MimeType = file.ContentType,
                AltText = altText,
                FileSize = file.Length,
                OriginalUrl = $"/api/images/{variants.ImageId}",
                ThumbnailUrl = variants.ThumbnailUrl,
                ResponsiveVariantUrls = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Variant400 = variants.Variant400Url,
                    Variant700 = variants.Variant700Url,
                    Variant1000 = variants.Variant1000Url
                }),
                UploadedByUserId = userIdGuid,
                UploadedAt = DateTimeOffset.UtcNow
            };

            await _imageRepository.CreateAsync(image, ct);

            var response = new ImageUploadResponse
            {
                ImageId = variants.ImageId,
                OriginalUrl = image.OriginalUrl,
                ThumbnailUrl = variants.ThumbnailUrl,
                Variant400Url = variants.Variant400Url,
                Variant700Url = variants.Variant700Url,
                Variant1000Url = variants.Variant1000Url,
                AltText = altText
            };

            _logger.LogInformation("Image uploaded successfully: {ImageId} by user {UserId}",
                variants.ImageId, userIdGuid);

            return CreatedAtAction(nameof(GetImage), new { id = variants.ImageId }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Image upload validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Image processing failed: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status422UnprocessableEntity,
                new { error = "Image processing failed. Please ensure the file is a valid image." });
        }
    }

    /// <summary>
    /// Serves the full-resolution original image.
    /// </summary>
    /// <param name="id">The image identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The image file stream.</returns>
    /// <response code="200">Image successfully retrieved.</response>
    /// <response code="404">Image not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(Guid id, CancellationToken ct)
    {
        var image = await _imageRepository.GetByIdAsync(id, ct);
        if (image == null)
        {
            return NotFound(new { error = "Image not found." });
        }

        try
        {
            var stream = await _fileStorageService.RetrieveAsync(image.OriginalUrl, ct);
            if (stream == null)
            {
                _logger.LogError("Image file not found on disk: {ImageId}", id);
                return NotFound(new { error = "Image file not found." });
            }

            return File(stream, image.MimeType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving image {ImageId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error retrieving image." });
        }
    }

    /// <summary>
    /// Serves the thumbnail variant (200x200 square crop).
    /// </summary>
    /// <param name="id">The image identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The thumbnail image file stream with immutable cache headers.</returns>
    /// <response code="200">Thumbnail successfully retrieved.</response>
    /// <response code="404">Image not found.</response>
    [HttpGet("{id}/thumbnail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThumbnail(Guid id, CancellationToken ct)
    {
        var image = await _imageRepository.GetByIdAsync(id, ct);
        if (image == null)
        {
            return NotFound(new { error = "Image not found." });
        }

        try
        {
            var stream = await _fileStorageService.RetrieveAsync(image.ThumbnailUrl, ct);
            if (stream == null)
            {
                _logger.LogError("Thumbnail file not found on disk: {ImageId}", id);
                return NotFound(new { error = "Thumbnail not found." });
            }

            Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return File(stream, image.MimeType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thumbnail {ImageId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error retrieving thumbnail." });
        }
    }

    /// <summary>
    /// Serves a responsive variant image (400, 700, or 1000 pixels wide).
    /// </summary>
    /// <param name="id">The image identifier.</param>
    /// <param name="width">The variant width (400, 700, or 1000).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The variant image file stream with immutable cache headers.</returns>
    /// <response code="200">Variant successfully retrieved.</response>
    /// <response code="400">Invalid variant width.</response>
    /// <response code="404">Image or variant not found.</response>
    [HttpGet("{id}/variants/{width}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVariant(Guid id, int width, CancellationToken ct)
    {
        // Validate width
        if (width != 400 && width != 700 && width != 1000)
        {
            return BadRequest(new { error = "Variant width must be 400, 700, or 1000 pixels." });
        }

        var image = await _imageRepository.GetByIdAsync(id, ct);
        if (image == null)
        {
            return NotFound(new { error = "Image not found." });
        }

        try
        {
            // Parse responsive variant URLs from JSON
            var variantUrls = System.Text.Json.JsonDocument.Parse(image.ResponsiveVariantUrls);
            var variantUrl = width switch
            {
                400 => variantUrls.RootElement.GetProperty("Variant400").GetString(),
                700 => variantUrls.RootElement.GetProperty("Variant700").GetString(),
                1000 => variantUrls.RootElement.GetProperty("Variant1000").GetString(),
                _ => null
            };

            if (string.IsNullOrEmpty(variantUrl))
            {
                _logger.LogError("Variant URL not found for image {ImageId} width {Width}", id, width);
                return NotFound(new { error = "Variant not found." });
            }

            var stream = await _fileStorageService.RetrieveAsync(variantUrl, ct);
            if (stream == null)
            {
                _logger.LogError("Variant file not found on disk: {ImageId} width {Width}", id, width);
                return NotFound(new { error = "Variant file not found." });
            }

            Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return File(stream, image.MimeType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving variant {ImageId} width {Width}", id, width);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error retrieving variant." });
        }
    }
}
