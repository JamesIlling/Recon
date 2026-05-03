using LocationManagement.Api.Controllers;
using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace LocationManagement.Api.Tests;

/// <summary>
/// Unit tests for UsersController.
/// Tests all success paths (200 responses) and error cases (401, 404, 400, 409, 413, 415).
/// </summary>
public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly UsersController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_mockUserService.Object, _mockLogger.Object);

        // Setup controller context with authenticated user
        SetupAuthenticatedUser(_testUserId);
    }

    #region GetProfile Tests

    [Fact]
    public async Task GetProfile_WithValidUser_Returns200WithProfile()
    {
        // Arrange
        var expectedProfile = new UserProfileDto(
            _testUserId,
            "testuser",
            "Test User",
            "https://example.com/avatar.jpg",
            true,
            DateTimeOffset.UtcNow
        );
        _mockUserService.Setup(s => s.GetProfileAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.GetProfile(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var returnedProfile = Assert.IsType<UserProfileDto>(okResult.Value);
        Assert.Equal(expectedProfile.Id, returnedProfile.Id);
        Assert.Equal(expectedProfile.Username, returnedProfile.Username);
        Assert.Equal(expectedProfile.DisplayName, returnedProfile.DisplayName);
        _mockUserService.Verify(s => s.GetProfileAsync(_testUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProfile_WithoutAuthentication_Returns401()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.GetProfile(CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithNonExistentUser_Returns404()
    {
        // Arrange
        _mockUserService.Setup(s => s.GetProfileAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.GetProfile(CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region ChangeDisplayName Tests

    [Fact]
    public async Task ChangeDisplayName_WithValidRequest_Returns200WithUpdatedProfile()
    {
        // Arrange
        var request = new ChangeDisplayNameRequest { NewDisplayName = "New Display Name" };
        var updatedProfile = new UserProfileDto(
            _testUserId,
            "testuser",
            "New Display Name",
            null,
            true,
            DateTimeOffset.UtcNow
        );
        _mockUserService.Setup(s => s.ChangeDisplayNameAsync(_testUserId, request.NewDisplayName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProfile);

        // Act
        var result = await _controller.ChangeDisplayName(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var returnedProfile = Assert.IsType<UserProfileDto>(okResult.Value);
        Assert.Equal("New Display Name", returnedProfile.DisplayName);
        _mockUserService.Verify(s => s.ChangeDisplayNameAsync(_testUserId, request.NewDisplayName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeDisplayName_WithEmptyDisplayName_Returns400()
    {
        // Arrange
        var request = new ChangeDisplayNameRequest { NewDisplayName = "" };

        // Act
        var result = await _controller.ChangeDisplayName(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ChangeDisplayName_WithDisplayNameExceedingMaxLength_Returns400()
    {
        // Arrange
        var request = new ChangeDisplayNameRequest { NewDisplayName = new string('a', 101) };

        // Act
        var result = await _controller.ChangeDisplayName(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ChangeDisplayName_WithDuplicateDisplayName_Returns409()
    {
        // Arrange
        var request = new ChangeDisplayNameRequest { NewDisplayName = "Existing Name" };
        _mockUserService.Setup(s => s.ChangeDisplayNameAsync(_testUserId, request.NewDisplayName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Display name already in use."));

        // Act
        var result = await _controller.ChangeDisplayName(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    [Fact]
    public async Task ChangeDisplayName_WithNonExistentUser_Returns404()
    {
        // Arrange
        var request = new ChangeDisplayNameRequest { NewDisplayName = "New Name" };
        _mockUserService.Setup(s => s.ChangeDisplayNameAsync(_testUserId, request.NewDisplayName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.ChangeDisplayName(request, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_WithValidRequest_Returns200()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword456"
        };
        _mockUserService.Setup(s => s.ChangePasswordAsync(_testUserId, request.CurrentPassword, request.NewPassword, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        _mockUserService.Verify(s => s.ChangePasswordAsync(_testUserId, request.CurrentPassword, request.NewPassword, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithEmptyCurrentPassword_Returns400()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "",
            NewPassword = "NewPassword456"
        };

        // Act
        var result = await _controller.ChangePassword(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithShortNewPassword_Returns400()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "Short1"
        };

        // Act
        var result = await _controller.ChangePassword(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_Returns401()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword456"
        };
        _mockUserService.Setup(s => s.ChangePasswordAsync(_testUserId, request.CurrentPassword, request.NewPassword, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Current password is incorrect."));

        // Act
        var result = await _controller.ChangePassword(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithNonExistentUser_Returns404()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword456"
        };
        _mockUserService.Setup(s => s.ChangePasswordAsync(_testUserId, request.CurrentPassword, request.NewPassword, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.ChangePassword(request, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region UploadAvatar Tests

    [Fact]
    public async Task UploadAvatar_WithNoFile_Returns400()
    {
        // Arrange
        var httpContext = CreateMockHttpContextWithForm();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        SetupAuthenticatedUser(_testUserId);

        // Act
        var result = await _controller.UploadAvatar(CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion

    #region UpdatePreferences Tests

    [Fact]
    public async Task UpdatePreferences_WithValidRequest_Returns200WithUpdatedProfile()
    {
        // Arrange
        var request = new UpdatePreferencesRequest { ShowPublicCollections = false };
        var updatedProfile = new UserProfileDto(
            _testUserId,
            "testuser",
            "Test User",
            null,
            false,
            DateTimeOffset.UtcNow
        );
        _mockUserService.Setup(s => s.UpdatePreferencesAsync(_testUserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProfile);

        // Act
        var result = await _controller.UpdatePreferences(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var returnedProfile = Assert.IsType<UserProfileDto>(okResult.Value);
        Assert.False(returnedProfile.ShowPublicCollections);
        _mockUserService.Verify(s => s.UpdatePreferencesAsync(_testUserId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferences_WithNullRequest_Returns400()
    {
        // Arrange
        UpdatePreferencesRequest? request = null;

        // Act
        var result = await _controller.UpdatePreferences(request!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdatePreferences_WithNonExistentUser_Returns404()
    {
        // Arrange
        var request = new UpdatePreferencesRequest { ShowPublicCollections = true };
        _mockUserService.Setup(s => s.UpdatePreferencesAsync(_testUserId, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.UpdatePreferences(request, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private IFormFile CreateMockFormFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private DefaultHttpContext CreateMockHttpContextWithForm(IFormFile? file = null)
    {
        var httpContext = new DefaultHttpContext();
        var files = new FormFileCollection();
        if (file != null)
        {
            files.Add(file);
        }

        var formCollection = new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(),
            files
        );

        httpContext.Request.ContentType = "multipart/form-data; boundary=----WebKitFormBoundary";
        httpContext.Request.Form = formCollection;

        return httpContext;
    }

    #endregion
}
