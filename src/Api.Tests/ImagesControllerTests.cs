using System.Security.Claims;
using LocationManagement.Api.Controllers;
using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Repositories;
using LocationManagement.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocationManagement.Api.Tests;

/// <summary>
/// Unit tests for ImagesController covering upload, retrieval, and variant serving.
/// </summary>
public class ImagesControllerTests
{
    private readonly Mock<IImageRepository> _mockImageRepository;
    private readonly Mock<IImageProcessingService> _mockImageProcessingService;
    private readonly Mock<ILocalFileStorageService> _mockFileStorageService;
    private readonly Mock<ILogger<ImagesController>> _mockLogger;
    private readonly ImagesController _controller;

    public ImagesControllerTests()
    {
        _mockImageRepository = new Mock<IImageRepository>();
        _mockImageProcessingService = new Mock<IImageProcessingService>();
        _mockFileStorageService = new Mock<ILocalFileStorageService>();
        _mockLogger = new Mock<ILogger<ImagesController>>();

        _controller = new ImagesController(
            _mockImageRepository.Object,
            _mockImageProcessingService.Object,
            _mockFileStorageService.Object,
            _mockLogger.Object);

        // Setup controller context with authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "Bearer"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region Upload Tests

    [Fact]
    public async Task Upload_WithValidJpegFile_ReturnsCreatedAtActionWithImageUploadResponse()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var fileName = "test.jpg";
        var altText = "Test image";
        var fileContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.Length).Returns(fileContent.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        var variants = new ImageVariants(
            imageId,
            "/api/images/thumb",
            "/api/images/400",
            "/api/images/700",
            "/api/images/1000");

        _mockImageProcessingService
            .Setup(x => x.ProcessAndStoreAsync(It.IsAny<Stream>(), "image/jpeg", altText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variants);

        // Act
        var result = await _controller.Upload(mockFile.Object, altText, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ImagesController.GetImage), createdResult.ActionName);

        var response = Assert.IsType<ImageUploadResponse>(createdResult.Value);
        Assert.Equal(imageId, response.ImageId);
        Assert.Equal(altText, response.AltText);

        _mockImageRepository.Verify(x => x.CreateAsync(It.IsAny<Image>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_WithValidPngFile_ReturnsCreatedAtAction()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var fileName = "test.png";
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns("image/png");
        mockFile.Setup(f => f.Length).Returns(fileContent.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        var variants = new ImageVariants(
            imageId,
            "/api/images/thumb",
            "/api/images/400",
            "/api/images/700",
            "/api/images/1000");

        _mockImageProcessingService
            .Setup(x => x.ProcessAndStoreAsync(It.IsAny<Stream>(), "image/png", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variants);

        // Act
        var result = await _controller.Upload(mockFile.Object, null, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
    }

    [Fact]
    public async Task Upload_WithNullFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Upload(null!, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Upload_WithAltTextExceeding500Chars_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.Length).Returns(1024);

        var longAltText = new string('a', 501);

        // Act
        var result = await _controller.Upload(mockFile.Object, longAltText, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Upload_WithUnsupportedMimeType_ReturnsUnsupportedMediaType()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.gif");
        mockFile.Setup(f => f.ContentType).Returns("image/gif");
        mockFile.Setup(f => f.Length).Returns(1024);

        // Act
        var result = await _controller.Upload(mockFile.Object, null, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Upload_WithFileSizeExceeding10MB_ReturnsPayloadTooLarge()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.Length).Returns(11 * 1024 * 1024); // 11 MB

        // Act
        var result = await _controller.Upload(mockFile.Object, null, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Upload_WhenImageProcessingFails_ReturnsUnprocessableEntity()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 0xFF, 0xD8 }));

        _mockImageProcessingService
            .Setup(x => x.ProcessAndStoreAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Image processing failed"));

        // Act
        var result = await _controller.Upload(mockFile.Object, null, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetImage Tests

    [Fact]
    public async Task GetImage_WithExistingImage_ReturnsFileResult()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var image = new Image
        {
            Id = imageId,
            FileName = "test.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024,
            OriginalUrl = "/images/test.jpg",
            ThumbnailUrl = "/images/test-thumb.jpg",
            ResponsiveVariantUrls = """{"Variant400":"/images/test-400.jpg","Variant700":"/images/test-700.jpg","Variant1000":"/images/test-1000.jpg"}""",
            UploadedByUserId = Guid.NewGuid(),
            UploadedAt = DateTimeOffset.UtcNow
        };

        var fileStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        _mockImageRepository
            .Setup(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        _mockFileStorageService
            .Setup(x => x.RetrieveAsync(image.OriginalUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        // Act
        var result = await _controller.GetImage(imageId, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
    }

    [Fact]
    public async Task GetImage_WithNonExistentImage_ReturnsNotFound()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        _mockImageRepository
            .Setup(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);

        // Act
        var result = await _controller.GetImage(imageId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region GetThumbnail Tests

    [Fact]
    public async Task GetThumbnail_WithExistingImage_ReturnsFileResultWithCacheHeaders()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var image = new Image
        {
            Id = imageId,
            FileName = "test.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024,
            OriginalUrl = "/images/test.jpg",
            ThumbnailUrl = "/images/test-thumb.jpg",
            ResponsiveVariantUrls = """{"Variant400":"/images/test-400.jpg","Variant700":"/images/test-700.jpg","Variant1000":"/images/test-1000.jpg"}""",
            UploadedByUserId = Guid.NewGuid(),
            UploadedAt = DateTimeOffset.UtcNow
        };

        var fileStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        _mockImageRepository
            .Setup(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        _mockFileStorageService
            .Setup(x => x.RetrieveAsync(image.ThumbnailUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        // Act
        var result = await _controller.GetThumbnail(imageId, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        Assert.Equal("public, max-age=31536000, immutable", _controller.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task GetThumbnail_WithNonExistentImage_ReturnsNotFound()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        _mockImageRepository
            .Setup(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);

        // Act
        var result = await _controller.GetThumbnail(imageId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region GetVariant Tests

    [Theory]
    [InlineData(400)]
    [InlineData(700)]
    [InlineData(1000)]
    public async Task GetVariant_WithValidWidth_ReturnsFileResultWithCacheHeaders(int width)
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var image = new Image
        {
            Id = imageId,
            FileName = "test.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024,
            OriginalUrl = "/images/test.jpg",
            ThumbnailUrl = "/images/test-thumb.jpg",
            ResponsiveVariantUrls = """{"Variant400":"/images/test-400.jpg","Variant700":"/images/test-700.jpg","Variant1000":"/images/test-1000.jpg"}""",
            UploadedByUserId = Guid.NewGuid(),
            UploadedAt = DateTimeOffset.UtcNow
        };

        var fileStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        _mockImageRepository
            .Setup(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        _mockFileStorageService
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        // Act
        var result = await _controller.GetVariant(imageId, width, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        Assert.Equal("public, max-age=31536000, immutable", _controller.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task GetVariant_WithInvalidWidth_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetVariant(Guid.NewGuid(), 500, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetVariant_WithNonExistentImage_ReturnsNotFound()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        _mockImageRepository
            .Setup(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);

        // Act
        var result = await _controller.GetVariant(imageId, 400, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion
}
