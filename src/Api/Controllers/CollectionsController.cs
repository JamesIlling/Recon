using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LocationManagement.Api.Controllers;

/// <summary>
/// API controller for managing LocationCollections.
/// </summary>
[ApiController]
[Route("api/collections")]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collectionService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CollectionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionsController"/> class.
    /// </summary>
    public CollectionsController(
        ICollectionService collectionService,
        ICacheService cacheService,
        ILogger<CollectionsController> logger)
    {
        _collectionService = collectionService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of public LocationCollections.
    /// </summary>
    /// <param name="page">The page number (1-indexed, default 1).</param>
    /// <param name="pageSize">The number of items per page (default 20, max 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated list of public collections.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<CollectionListItemDto>>> ListPublic(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var (totalCount, items) = await _collectionService.ListPublicAsync(page, pageSize, ct);

            var dtos = items.Select(c => new CollectionListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                OwnerDisplayName = c.Owner.DisplayName,
                ThumbnailUrl = c.ThumbnailImage != null ? $"/api/images/{c.ThumbnailImage.Id}/thumbnail" : null,
                CreatedAt = c.CreatedAt
            }).ToList();

            return Ok(new PaginatedResponse<CollectionListItemDto>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = dtos
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a paginated list of public collections and collections owned by the authenticated user.
    /// Never cached. Requires authentication.
    /// </summary>
    /// <param name="page">The page number (1-indexed, default 1).</param>
    /// <param name="pageSize">The number of items per page (default 20, max 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated list of collections with ownership information.</returns>
    [HttpGet("combined")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<CollectionCombinedDto>>> ListCombined(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token."));

            var (totalCount, items) = await _collectionService.ListCombinedAsync(userId, page, pageSize, ct);

            var dtos = items.Select(item => new CollectionCombinedDto
            {
                Id = item.Collection.Id,
                Name = item.Collection.Name,
                Description = item.Collection.Description,
                OwnerDisplayName = item.Collection.Owner.DisplayName,
                ThumbnailUrl = item.Collection.ThumbnailImage != null ? $"/api/images/{item.Collection.ThumbnailImage.Id}/thumbnail" : null,
                IsPublic = item.Collection.IsPublic,
                IsOwner = item.IsOwner,
                CreatedAt = item.Collection.CreatedAt
            }).ToList();

            return Ok(new PaginatedResponse<CollectionCombinedDto>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = dtos
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new LocationCollection. Requires authentication.
    /// </summary>
    /// <param name="request">The collection creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created collection.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CollectionDetailDto>> Create(
        [FromBody] CreateCollectionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token."));

            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var collection = await _collectionService.CreateAsync(
                request.Name,
                request.Description,
                request.IsPublic,
                request.BoundingShapeId,
                request.ThumbnailImageId,
                userId,
                sourceIp,
                ct);

            var dto = MapToDetailDto(collection);
            return CreatedAtAction(nameof(GetById), new { id = collection.Id }, dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a LocationCollection by ID.
    /// Public collections are accessible to anyone; private collections are only accessible to the owner.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The collection details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionDetailDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        Guid? userId = null;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null)
        {
            userId = Guid.Parse(userIdClaim.Value);
        }

        var collection = await _collectionService.GetByIdAsync(id, userId, ct);

        if (collection == null)
            return NotFound();

        var dto = MapToDetailDto(collection);
        return Ok(dto);
    }

    /// <summary>
    /// Updates a LocationCollection. Owner only.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated collection.</returns>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionDetailDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCollectionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token."));

            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var collection = await _collectionService.UpdateAsync(
                id,
                request.Name,
                request.Description,
                request.IsPublic,
                request.BoundingShapeId,
                request.ThumbnailImageId,
                userId,
                sourceIp,
                ct);

            var dto = MapToDetailDto(collection);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Deletes a LocationCollection. Owner or admin only.
    /// </summary>
    /// <param name="id">The collection ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token."));

            var isAdmin = User.IsInRole("Admin");
            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _collectionService.DeleteAsync(id, userId, isAdmin, sourceIp, ct);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Adds a Location to a LocationCollection.
    /// If the requesting user is the owner, the location is added directly.
    /// Otherwise, a pending membership request is created.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="request">The add member request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created membership or pending request.</returns>
    [HttpPost("{collectionId}/members")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember(
        [FromRoute] Guid collectionId,
        [FromBody] AddMemberRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token."));

            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await _collectionService.AddMemberAsync(collectionId, request.LocationId, userId, sourceIp, ct);

            return CreatedAtAction(nameof(GetById), new { id = collectionId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Removes a Location from a LocationCollection. Owner only.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="locationId">The location ID to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{collectionId}/members/{locationId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] Guid collectionId,
        [FromRoute] Guid locationId,
        CancellationToken ct = default)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token."));

            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _collectionService.RemoveMemberAsync(collectionId, locationId, userId, sourceIp, ct);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Gets all pending membership requests for a collection. Owner only.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of pending membership requests.</returns>
    [HttpGet("{collectionId}/pending-members")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PendingMembershipDto>>> GetPendingMemberships(
        [FromRoute] Guid collectionId,
        CancellationToken ct = default)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token."));

            var requests = await _collectionService.GetPendingMembershipsAsync(collectionId, userId, ct);

            var dtos = requests.Select(r => new PendingMembershipDto
            {
                Id = r.Id,
                LocationId = r.LocationId,
                LocationName = r.Location.Name,
                RequestedByUserId = r.RequestedByUserId,
                RequestedByDisplayName = r.RequestedByUser.DisplayName,
                RequestedAt = r.RequestedAt
            }).ToList();

            return Ok(dtos);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Approves a pending membership request. Owner only.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="requestId">The pending membership request ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{collectionId}/pending-members/{requestId}/approve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveMembership(
        [FromRoute] Guid collectionId,
        [FromRoute] Guid requestId,
        CancellationToken ct = default)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token."));

            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _collectionService.ApproveMembershipAsync(collectionId, requestId, userId, sourceIp, ct);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Rejects a pending membership request. Owner only.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="requestId">The pending membership request ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{collectionId}/pending-members/{requestId}/reject")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectMembership(
        [FromRoute] Guid collectionId,
        [FromRoute] Guid requestId,
        CancellationToken ct = default)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token."));

            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _collectionService.RejectMembershipAsync(collectionId, requestId, userId, sourceIp, ct);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private static CollectionDetailDto MapToDetailDto(LocationCollection collection)
    {
        return new CollectionDetailDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            OwnerDisplayName = collection.Owner.DisplayName,
            ThumbnailUrl = collection.ThumbnailImage != null ? $"/api/images/{collection.ThumbnailImage.Id}/thumbnail" : null,
            BoundingShapeId = collection.BoundingShapeId,
            IsPublic = collection.IsPublic,
            MemberCount = collection.Members.Count,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt
        };
    }
}

/// <summary>
/// Request DTO for creating a collection.
/// </summary>
public class CreateCollectionRequest
{
    /// <summary>Gets or sets the collection name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the collection is public.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Gets or sets the optional bounding shape ID.</summary>
    public Guid? BoundingShapeId { get; set; }

    /// <summary>Gets or sets the optional thumbnail image ID.</summary>
    public Guid? ThumbnailImageId { get; set; }
}

/// <summary>
/// Request DTO for updating a collection.
/// </summary>
public class UpdateCollectionRequest
{
    /// <summary>Gets or sets the collection name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the collection is public.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Gets or sets the optional bounding shape ID.</summary>
    public Guid? BoundingShapeId { get; set; }

    /// <summary>Gets or sets the optional thumbnail image ID.</summary>
    public Guid? ThumbnailImageId { get; set; }
}

/// <summary>
/// Request DTO for adding a member to a collection.
/// </summary>
public class AddMemberRequest
{
    /// <summary>Gets or sets the location ID to add.</summary>
    public required Guid LocationId { get; set; }
}

/// <summary>
/// Response DTO for collection list items.
/// </summary>
public class CollectionListItemDto
{
    /// <summary>Gets or sets the collection ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the collection name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the collection description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the owner's display name.</summary>
    public required string OwnerDisplayName { get; set; }

    /// <summary>Gets or sets the thumbnail URL.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Response DTO for combined collection list (public + owned).
/// </summary>
public class CollectionCombinedDto
{
    /// <summary>Gets or sets the collection ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the collection name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the collection description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the owner's display name.</summary>
    public required string OwnerDisplayName { get; set; }

    /// <summary>Gets or sets the thumbnail URL.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Gets or sets whether the collection is public.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Gets or sets whether the requesting user is the owner.</summary>
    public bool IsOwner { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Response DTO for collection detail.
/// </summary>
public class CollectionDetailDto
{
    /// <summary>Gets or sets the collection ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the collection name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the collection description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the owner's display name.</summary>
    public required string OwnerDisplayName { get; set; }

    /// <summary>Gets or sets the thumbnail URL.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Gets or sets the bounding shape ID.</summary>
    public Guid? BoundingShapeId { get; set; }

    /// <summary>Gets or sets whether the collection is public.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Gets or sets the member count.</summary>
    public int MemberCount { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Response DTO for pending membership requests.
/// </summary>
public class PendingMembershipDto
{
    /// <summary>Gets or sets the request ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the location ID.</summary>
    public Guid LocationId { get; set; }

    /// <summary>Gets or sets the location name.</summary>
    public required string LocationName { get; set; }

    /// <summary>Gets or sets the requesting user ID.</summary>
    public Guid RequestedByUserId { get; set; }

    /// <summary>Gets or sets the requesting user's display name.</summary>
    public required string RequestedByDisplayName { get; set; }

    /// <summary>Gets or sets the request timestamp.</summary>
    public DateTimeOffset RequestedAt { get; set; }
}

/// <summary>
/// Generic paginated response wrapper.
/// </summary>
public class PaginatedResponse<T>
{
    /// <summary>Gets or sets the total count of items.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets the current page number.</summary>
    public int Page { get; set; }

    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets or sets the items for this page.</summary>
    public List<T> Items { get; set; } = [];
}
