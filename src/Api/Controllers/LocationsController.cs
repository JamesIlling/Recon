using System.Security.Claims;
using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Services;
using LocationManagement.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LocationEntity = LocationManagement.Api.Models.Entities.Location;

namespace LocationManagement.Api.Controllers;

/// <summary>
/// Provides endpoints for Location CRUD operations and approval workflow management.
/// </summary>
[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationService locationService,
        ICacheService cacheService,
        ILogger<LocationsController> logger)
    {
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a paginated list of all Locations.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListLocations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1)
            return BadRequest(new { error = "Page must be >= 1." });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "PageSize must be between 1 and 100." });

        try
        {
            var (total, locations) = await _locationService.ListAsync(page, pageSize, ct);

            return Ok(new
            {
                totalCount = total,
                page,
                pageSize,
                items = locations.Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Latitude,
                    l.Longitude,
                    l.SourceSrid,
                    creatorDisplayName = l.Creator?.DisplayName,
                    l.CreatedAt
                })
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid pagination parameters: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing locations");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error retrieving locations." });
        }
    }

    /// <summary>
    /// Creates a new Location.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateLocation(
        [FromBody] CreateLocationRequest request,
        CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new { error = "Request body is required." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userIdGuid))
            return Unauthorized(new { error = "Invalid user context." });

        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            var location = await _locationService.CreateAsync(
                request.Name,
                request.Latitude,
                request.Longitude,
                request.SourceSrid,
                request.ContentSequence,
                userIdGuid,
                sourceIp,
                ct);

            await _cacheService.InvalidateByPrefixAsync("locations:list:", ct);

            return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, new
            {
                location.Id,
                location.Name,
                location.Latitude,
                location.Longitude,
                location.SourceSrid,
                location.ContentSequence,
                creatorDisplayName = location.Creator?.DisplayName,
                location.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Location creation validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error creating location." });
        }
    }

    /// <summary>
    /// Retrieves a Location by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocation(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userIdGuid = null;
            if (Guid.TryParse(userId, out var parsed))
                userIdGuid = parsed;

            var result = await _locationService.GetByIdAsync(id, userIdGuid, ct);
            if (result == null)
                return NotFound(new { error = "Location not found." });

            return Ok(new
            {
                result.Id,
                result.Name,
                result.Latitude,
                result.Longitude,
                result.SourceSrid,
                result.ContentSequence,
                creatorDisplayName = result.Creator?.DisplayName,
                result.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location {LocationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error retrieving location." });
        }
    }

    /// <summary>
    /// Updates a Location (creator path - immediate update).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation(
        Guid id,
        [FromBody] CreateLocationRequest request,
        CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new { error = "Request body is required." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userIdGuid))
            return Unauthorized(new { error = "Invalid user context." });

        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            var location = await _locationService.UpdateAsync(
                id,
                request.Name,
                request.Latitude,
                request.Longitude,
                request.SourceSrid,
                request.ContentSequence,
                userIdGuid,
                sourceIp,
                ct);

            await _cacheService.InvalidateAsync($"locations:detail:{id}", ct);
            await _cacheService.InvalidateByPrefixAsync("locations:list:", ct);

            return Ok(new
            {
                location.Id,
                location.Name,
                location.Latitude,
                location.Longitude,
                location.SourceSrid,
                location.ContentSequence,
                creatorDisplayName = location.Creator?.DisplayName,
                location.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Location update validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Location update error: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location {LocationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error updating location." });
        }
    }

    /// <summary>
    /// Deletes a Location (creator or admin only).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLocation(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userIdGuid))
            return Unauthorized(new { error = "Invalid user context." });

        var isAdmin = User.IsInRole("Admin");
        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            await _locationService.DeleteAsync(id, userIdGuid, isAdmin, sourceIp, ct);

            await _cacheService.InvalidateAsync($"locations:detail:{id}", ct);
            await _cacheService.InvalidateByPrefixAsync("locations:list:", ct);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Location deletion error: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting location {LocationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error deleting location." });
        }
    }

    /// <summary>
    /// Submits a pending edit for a Location (non-creator path).
    /// </summary>
    [HttpPut("{id}/pending-edit")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitPendingEdit(
        Guid id,
        [FromBody] CreateLocationRequest request,
        CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new { error = "Request body is required." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userIdGuid))
            return Unauthorized(new { error = "Invalid user context." });

        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            var pendingEdit = await _locationService.SubmitPendingEditAsync(
                id,
                request.Name,
                request.Latitude,
                request.Longitude,
                request.SourceSrid,
                request.ContentSequence,
                userIdGuid,
                sourceIp,
                ct);

            await _cacheService.InvalidateAsync($"locations:detail:{id}", ct);

            return Accepted(new
            {
                pendingEdit.Id,
                pendingEdit.LocationId,
                pendingEdit.Name,
                pendingEdit.Latitude,
                pendingEdit.Longitude,
                pendingEdit.SourceSrid,
                pendingEdit.ContentSequence,
                submittedByDisplayName = pendingEdit.SubmittedByUser?.DisplayName,
                pendingEdit.SubmittedAt
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Pending edit validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Pending edit submission error: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting pending edit for location {LocationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error submitting pending edit." });
        }
    }

    /// <summary>
    /// Retrieves all pending edits for a Location (creator only).
    /// </summary>
    [HttpGet("{id}/pending-edits")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPendingEdits(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userIdGuid))
            return Unauthorized(new { error = "Invalid user context." });

        try
        {
            var pendingEdits = await _locationService.GetPendingEditsAsync(id, userIdGuid, ct);

            return Ok(pendingEdits.Select(pe => new
            {
                pe.Id,
                pe.LocationId,
                pe.Name,
                pe.Latitude,
                pe.Longitude,
                pe.SourceSrid,
                pe.ContentSequence,
                submittedByDisplayName = pe.SubmittedByUser?.DisplayName,
                pe.SubmittedAt
            }));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Get pending edits error: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending edits for location {LocationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error retrieving pending edits." });
        }
    }

    /// <summary>
    /// Approves a pending edit (creator only).
    /// </summary>
    [HttpPost("{id}/pending-edits/{editId}/approve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApprovePendingEdit(Guid id, Guid editId, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userIdGuid))
            return Unauthorized(new { error = "Invalid user context." });

        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            var location = await _locationService.ApprovePendingEditAsync(id, editId, userIdGuid, sourceIp, ct);

            await _cacheService.InvalidateAsync($"locations:detail:{id}", ct);
            await _cacheService.InvalidateByPrefixAsync("locations:list:", ct);

            return Ok(new
            {
                location.Id,
                location.Name,
                location.Latitude,
                location.Longitude,
                location.SourceSrid,
                location.ContentSequence,
                creatorDisplayName = location.Creator?.DisplayName,
                location.CreatedAt
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Approve pending edit error: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving pending edit {EditId} for location {LocationId}", editId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error approving pending edit." });
        }
    }

    /// <summary>
    /// Rejects a pending edit (creator only).
    /// </summary>
    [HttpPost("{id}/pending-edits/{editId}/reject")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectPendingEdit(Guid id, Guid editId, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userIdGuid))
            return Unauthorized(new { error = "Invalid user context." });

        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            await _locationService.RejectPendingEditAsync(id, editId, userIdGuid, sourceIp, ct);

            await _cacheService.InvalidateAsync($"locations:detail:{id}", ct);
            await _cacheService.InvalidateByPrefixAsync("locations:list:", ct);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Reject pending edit error: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting pending edit {EditId} for location {LocationId}", editId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error rejecting pending edit." });
        }
    }
}
