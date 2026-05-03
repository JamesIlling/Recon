using System.Security.Claims;
using LocationManagement.Api.Controllers;
using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite.Geometries;
using Xunit;
using LocationEntity = LocationManagement.Api.Models.Entities.Location;

namespace LocationManagement.Api.Tests;

/// <summary>
/// Unit tests for LocationsController.
/// Tests all endpoints, authorization, error handling, and response codes.
/// </summary>
public class LocationsControllerTests
{
    private readonly Mock<ILocationService> _mockLocationService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<LocationsController>> _mockLogger;
    private readonly LocationsController _controller;
    private readonly GeometryFactory _gf = new(new PrecisionModel(), 4326);

    public LocationsControllerTests()
    {
        _mockLocationService = new Mock<ILocationService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<LocationsController>>();

        _controller = new LocationsController(
            _mockLocationService.Object,
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    private void SetupControllerUser(Guid userId, bool isAdmin = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private LocationEntity CreateTestLocation(Guid? id = null, Guid? creatorId = null)
    {
        return new LocationEntity
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Test Location",
            Latitude = 40.7128,
            Longitude = -74.0060,
            Coordinates = _gf.CreatePoint(new Coordinate(-74.0060, 40.7128)),
            SourceSrid = 4326,
            ContentSequence = "[]",
            CreatorId = creatorId ?? Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task ListLocations_WithValidPagination_Returns200WithLocations()
    {
        var locations = new List<LocationEntity>
        {
            CreateTestLocation(),
            CreateTestLocation()
        };

        _mockLocationService
            .Setup(x => x.ListAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((2, locations));

        var result = await _controller.ListLocations(1, 20, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task ListLocations_WithInvalidPage_Returns400()
    {
        var result = await _controller.ListLocations(0, 20, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ListLocations_WithInvalidPageSize_Returns400()
    {
        var result = await _controller.ListLocations(1, 101, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateLocation_WithValidInput_Returns201Created()
    {
        var userId = Guid.NewGuid();
        SetupControllerUser(userId);

        var request = new CreateLocationRequest
        {
            Name = "New Location",
            Latitude = 40.7128,
            Longitude = -74.0060,
            SourceSrid = 4326,
            ContentSequence = "[]"
        };

        var createdLocation = CreateTestLocation(creatorId: userId);

        _mockLocationService
            .Setup(x => x.CreateAsync(
                request.Name, request.Latitude, request.Longitude, request.SourceSrid,
                request.ContentSequence, userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdLocation);

        var result = await _controller.CreateLocation(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(LocationsController.GetLocation), createdResult.ActionName);
    }

    [Fact]
    public async Task CreateLocation_WithoutAuthentication_Returns401()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateLocationRequest
        {
            Name = "New Location",
            Latitude = 40.7128,
            Longitude = -74.0060,
            SourceSrid = 4326,
            ContentSequence = "[]"
        };

        var result = await _controller.CreateLocation(request, CancellationToken.None);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task CreateLocation_WithValidationError_Returns400()
    {
        var userId = Guid.NewGuid();
        SetupControllerUser(userId);

        var request = new CreateLocationRequest
        {
            Name = "New Location",
            Latitude = 91.0,
            Longitude = -74.0060,
            SourceSrid = 4326,
            ContentSequence = "[]"
        };

        _mockLocationService
            .Setup(x => x.CreateAsync(
                It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid latitude"));

        var result = await _controller.CreateLocation(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetLocation_WithValidId_Returns200()
    {
        var locationId = Guid.NewGuid();
        var location = CreateTestLocation(locationId);

        _mockLocationService
            .Setup(x => x.GetByIdAsync(locationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        // Set up controller context with no user (unauthenticated)
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.GetLocation(locationId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task GetLocation_WithNonExistentId_Returns404()
    {
        var locationId = Guid.NewGuid();

        _mockLocationService
            .Setup(x => x.GetByIdAsync(locationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocationEntity?)null);

        // Set up controller context with no user (unauthenticated)
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.GetLocation(locationId, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdateLocation_AsCreator_Returns200()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        SetupControllerUser(userId);

        var request = new CreateLocationRequest
        {
            Name = "Updated Location",
            Latitude = 51.5074,
            Longitude = -0.1278,
            SourceSrid = 4326,
            ContentSequence = "[]"
        };

        var updatedLocation = CreateTestLocation(locationId, userId);

        _mockLocationService
            .Setup(x => x.UpdateAsync(
                locationId, request.Name, request.Latitude, request.Longitude, request.SourceSrid,
                request.ContentSequence, userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedLocation);

        var result = await _controller.UpdateLocation(locationId, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task UpdateLocation_AsNonCreator_Returns403()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        SetupControllerUser(userId);

        var request = new CreateLocationRequest
        {
            Name = "Updated Location",
            Latitude = 51.5074,
            Longitude = -0.1278,
            SourceSrid = 4326,
            ContentSequence = "[]"
        };

        _mockLocationService
            .Setup(x => x.UpdateAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Only the location creator may perform this operation."));

        var result = await _controller.UpdateLocation(locationId, request, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateLocation_WithoutAuthentication_Returns401()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateLocationRequest
        {
            Name = "Updated Location",
            Latitude = 51.5074,
            Longitude = -0.1278,
            SourceSrid = 4326,
            ContentSequence = "[]"
        };

        var result = await _controller.UpdateLocation(Guid.NewGuid(), request, CancellationToken.None);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task DeleteLocation_AsCreator_Returns204()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        SetupControllerUser(userId);

        _mockLocationService
            .Setup(x => x.DeleteAsync(locationId, userId, false, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteLocation(locationId, CancellationToken.None);

        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
    }

    [Fact]
    public async Task DeleteLocation_AsNonCreator_Returns403()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        SetupControllerUser(userId);

        _mockLocationService
            .Setup(x => x.DeleteAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Only the creator or an admin may delete a location."));

        var result = await _controller.DeleteLocation(locationId, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteLocation_WithoutAuthentication_Returns401()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.DeleteLocation(Guid.NewGuid(), CancellationToken.None);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task SubmitPendingEdit_AsNonCreator_Returns202Accepted()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        SetupControllerUser(userId);

        var request = new CreateLocationRequest
        {
            Name = "Proposed Name",
            Latitude = 40.7128,
            Longitude = -74.0060,
            SourceSrid = 4326,
            ContentSequence = "[]"
        };

        var pendingEdit = new PendingEdit
        {
            Id = Guid.NewGuid(),
            LocationId = locationId,
            SubmittedByUserId = userId,
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Coordinates = _gf.CreatePoint(new Coordinate(request.Longitude, request.Latitude)),
            SourceSrid = request.SourceSrid,
            ContentSequence = request.ContentSequence,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        _mockLocationService
            .Setup(x => x.SubmitPendingEditAsync(
                locationId, request.Name, request.Latitude, request.Longitude, request.SourceSrid,
                request.ContentSequence, userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingEdit);

        var result = await _controller.SubmitPendingEdit(locationId, request, CancellationToken.None);

        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(StatusCodes.Status202Accepted, acceptedResult.StatusCode);
    }

    [Fact]
    public async Task SubmitPendingEdit_WithoutAuthentication_Returns401()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateLocationRequest
        {
            Name = "Proposed Name",
            Latitude = 40.7128,
            Longitude = -74.0060,
            SourceSrid = 4326,
            ContentSequence = "[]"
        };

        var result = await _controller.SubmitPendingEdit(Guid.NewGuid(), request, CancellationToken.None);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task GetPendingEdits_AsCreator_Returns200()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        SetupControllerUser(userId);

        var pendingEdits = new List<PendingEdit>
        {
            new()
            {
                Id = Guid.NewGuid(),
                LocationId = locationId,
                SubmittedByUserId = Guid.NewGuid(),
                Name = "Edit",
                Latitude = 40.0,
                Longitude = -74.0,
                Coordinates = _gf.CreatePoint(new Coordinate(-74.0, 40.0)),
                SourceSrid = 4326,
                ContentSequence = "[]",
                SubmittedAt = DateTimeOffset.UtcNow
            }
        };

        _mockLocationService
            .Setup(x => x.GetPendingEditsAsync(locationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingEdits);

        var result = await _controller.GetPendingEdits(locationId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task GetPendingEdits_AsNonCreator_Returns403()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        SetupControllerUser(userId);

        _mockLocationService
            .Setup(x => x.GetPendingEditsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Only the location creator may perform this operation."));

        var result = await _controller.GetPendingEdits(locationId, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ApprovePendingEdit_AsCreator_Returns200()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var editId = Guid.NewGuid();
        SetupControllerUser(userId);

        var approvedLocation = CreateTestLocation(locationId, userId);

        _mockLocationService
            .Setup(x => x.ApprovePendingEditAsync(locationId, editId, userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedLocation);

        var result = await _controller.ApprovePendingEdit(locationId, editId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task ApprovePendingEdit_AsNonCreator_Returns403()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var editId = Guid.NewGuid();
        SetupControllerUser(userId);

        _mockLocationService
            .Setup(x => x.ApprovePendingEditAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Only the location creator may perform this operation."));

        var result = await _controller.ApprovePendingEdit(locationId, editId, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task RejectPendingEdit_AsCreator_Returns204()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var editId = Guid.NewGuid();
        SetupControllerUser(userId);

        _mockLocationService
            .Setup(x => x.RejectPendingEditAsync(locationId, editId, userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.RejectPendingEdit(locationId, editId, CancellationToken.None);

        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
    }

    [Fact]
    public async Task RejectPendingEdit_AsNonCreator_Returns403()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var editId = Guid.NewGuid();
        SetupControllerUser(userId);

        _mockLocationService
            .Setup(x => x.RejectPendingEditAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Only the location creator may perform this operation."));

        var result = await _controller.RejectPendingEdit(locationId, editId, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }
}
