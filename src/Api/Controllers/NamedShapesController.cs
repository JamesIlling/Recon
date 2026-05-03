using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocationManagement.Api.Controllers;

/// <summary>
/// Controller for NamedShape operations (upload, rename, delete, list).
/// All write operations require Admin role.
/// </summary>
[ApiController]
[Route("api/named-shapes")]
[Authorize]
public sealed class NamedShapesController : ControllerBase
{
    private readonly INamedShapeService _namedShapeService;
    private readonly ILogger<NamedShapesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedShapesController"/> class.
    /// </summary>
    /// <param name="namedShapeService">The NamedShape service.</param>
    /// <param name="logger">The logger.</param>
    public NamedShapesController(INamedShapeService namedShapeService, ILogger<NamedShapesController> logger)
    {
        _namedShapeService = namedShapeService ?? throw new ArgumentNullException(nameof(namedShapeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all NamedShapes with pagination.
    /// Authenticated users only. Returns id and name only.
    /// </summary>
    /// <param name="page">The page number (1-based, default 1).</param>
    /// <param name="pageSize">The number of items per page (default 20, max 100).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of NamedShapes.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<NamedShapeListItemDto>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest(new { error = "Page must be >= 1." });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "PageSize must be between 1 and 100." });

        try
        {
            var (totalCount, items) = await _namedShapeService.ListAsync(page, pageSize, cancellationToken);

            var dtos = items
                .Select(item => new NamedShapeListItemDto { Id = item.Id, Name = item.Name })
                .ToList();

            return Ok(new PaginatedResponse<NamedShapeListItemDto>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = dtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing NamedShapes");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while listing NamedShapes." });
        }
    }

    /// <summary>
    /// Uploads a new NamedShape from GeoJSON geometry.
    /// Admin-only operation.
    /// </summary>
    /// <param name="request">The upload request containing name and GeoJSON geometry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created NamedShape.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NamedShapeDto>> UploadAsync(
        [FromBody] UploadNamedShapeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest(new { error = "Request body is required." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        if (string.IsNullOrWhiteSpace(request.GeoJsonGeometry))
            return BadRequest(new { error = "GeoJsonGeometry is required." });

        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var adminUserId))
                return Unauthorized(new { error = "Invalid user context." });

            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var namedShape = await _namedShapeService.UploadAsync(
                request.Name,
                request.GeoJsonGeometry,
                adminUserId,
                sourceIp,
                cancellationToken);

            var dto = new NamedShapeDto
            {
                Id = namedShape.Id,
                Name = namedShape.Name,
                CreatedAt = namedShape.CreatedAt,
                CreatedByUserId = namedShape.CreatedByUserId
            };

            return CreatedAtAction(nameof(UploadAsync), new { id = namedShape.Id }, dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error uploading NamedShape: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation uploading NamedShape: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading NamedShape");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while uploading the NamedShape." });
        }
    }

    /// <summary>
    /// Renames an existing NamedShape.
    /// Admin-only operation.
    /// </summary>
    /// <param name="id">The ID of the NamedShape to rename.</param>
    /// <param name="request">The rename request containing the new name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated NamedShape.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NamedShapeDto>> RenameAsync(
        [FromRoute] Guid id,
        [FromBody] RenameNamedShapeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return BadRequest(new { error = "Invalid NamedShape ID." });

        if (request == null)
            return BadRequest(new { error = "Request body is required." });

        if (string.IsNullOrWhiteSpace(request.NewName))
            return BadRequest(new { error = "NewName is required." });

        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var adminUserId))
                return Unauthorized(new { error = "Invalid user context." });

            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var namedShape = await _namedShapeService.RenameAsync(
                id,
                request.NewName,
                adminUserId,
                sourceIp,
                cancellationToken);

            var dto = new NamedShapeDto
            {
                Id = namedShape.Id,
                Name = namedShape.Name,
                CreatedAt = namedShape.CreatedAt,
                CreatedByUserId = namedShape.CreatedByUserId
            };

            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error renaming NamedShape: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation renaming NamedShape: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming NamedShape");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while renaming the NamedShape." });
        }
    }

    /// <summary>
    /// Deletes a NamedShape.
    /// Admin-only operation. Fails if the shape is referenced by any LocationCollection.
    /// </summary>
    /// <param name="id">The ID of the NamedShape to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return BadRequest(new { error = "Invalid NamedShape ID." });

        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var adminUserId))
                return Unauthorized(new { error = "Invalid user context." });

            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _namedShapeService.DeleteAsync(id, adminUserId, sourceIp, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation deleting NamedShape: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting NamedShape");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while deleting the NamedShape." });
        }
    }
}

/// <summary>
/// Request DTO for uploading a NamedShape.
/// </summary>
public sealed class UploadNamedShapeRequest
{
    /// <summary>
    /// Gets or sets the name of the shape.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the GeoJSON geometry string (Polygon or MultiPolygon).
    /// </summary>
    public required string GeoJsonGeometry { get; set; }
}

/// <summary>
/// Request DTO for renaming a NamedShape.
/// </summary>
public sealed class RenameNamedShapeRequest
{
    /// <summary>
    /// Gets or sets the new name of the shape.
    /// </summary>
    public required string NewName { get; set; }
}

/// <summary>
/// Response DTO for a NamedShape.
/// </summary>
public sealed class NamedShapeDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the admin who created this shape.
    /// </summary>
    public required Guid CreatedByUserId { get; set; }
}

/// <summary>
/// Response DTO for a NamedShape list item (id and name only).
/// </summary>
public sealed class NamedShapeListItemDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public required string Name { get; set; }
}
