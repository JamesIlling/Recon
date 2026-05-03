using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;

namespace LocationManagement.Api.Controllers;

/// <summary>
/// Provides admin-only endpoints for user management and audit log access.
/// All endpoints require JWT authentication with Admin role.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserAdminService _userAdminService;
    private readonly IAuditService _auditService;
    private readonly IBackupService _backupService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserAdminService userAdminService,
        IAuditService auditService,
        IBackupService backupService,
        ILogger<AdminController> logger)
    {
        _userAdminService = userAdminService ?? throw new ArgumentNullException(nameof(userAdminService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all users with pagination.
    /// </summary>
    /// <param name="page">The page number (1-indexed). Defaults to 1.</param>
    /// <param name="pageSize">The number of users per page. Defaults to 20, max 100.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated list of users with their roles.</returns>
    /// <response code="200">Users retrieved successfully.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="403">Non-admin user attempted access.</response>
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1)
        {
            return BadRequest(new { error = "Page must be 1 or greater." });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { error = "Page size must be between 1 and 100." });
        }

        try
        {
            var (users, totalCount) = await _userAdminService.ListUsersAsync(page, pageSize, ct);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            _logger.LogInformation("Admin listed users: page {Page}, pageSize {PageSize}", page, pageSize);

            return Ok(new
            {
                users,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages
                }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid pagination parameters: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Changes a user's role (promote or demote).
    /// </summary>
    /// <param name="id">The ID of the user whose role is to be changed.</param>
    /// <param name="request">The role change request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user DTO.</returns>
    /// <response code="200">Role changed successfully.</response>
    /// <response code="400">Invalid role or validation error.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="403">Non-admin user attempted access.</response>
    /// <response code="404">User not found.</response>
    /// <response code="409">Cannot demote the last admin.</response>
    [HttpPut("users/{id}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeUserRole(
        [FromRoute] Guid id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest(new { error = "Role is required." });
        }

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var newRole))
        {
            return BadRequest(new { error = $"Invalid role. Must be one of: {string.Join(", ", Enum.GetNames(typeof(UserRole)))}" });
        }

        var actingUserId = GetAuthenticatedUserId();
        if (actingUserId == Guid.Empty)
        {
            _logger.LogWarning("ChangeUserRole called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        try
        {
            var updatedUser = newRole == UserRole.Admin
                ? await _userAdminService.PromoteAsync(id, actingUserId, ct)
                : await _userAdminService.DemoteAsync(id, actingUserId, ct);

            _logger.LogInformation("User {UserId} role changed to {Role} by admin {ActingUserId}", id, newRole, actingUserId);
            return Ok(updatedUser);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Role change requested for non-existent user {UserId}", id);
            return NotFound(new { error = "User not found." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Role change failed: {Message}", ex.Message);

            // Check if this is the last admin demotion error
            if (ex.Message.Contains("last admin"))
            {
                return Conflict(new { error = ex.Message });
            }

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Extracts the authenticated user's ID from the JWT claims.
    /// </summary>
    /// <returns>The user ID, or Guid.Empty if not found.</returns>
    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Retrieves the audit log with optional filtering and pagination.
    /// </summary>
    /// <param name="eventType">Filter by event type (optional).</param>
    /// <param name="actingUserId">Filter by the user who performed the action (optional).</param>
    /// <param name="resourceType">Filter by resource type (Location, User, Collection, etc.) (optional).</param>
    /// <param name="resourceId">Filter by specific resource ID (optional).</param>
    /// <param name="outcome">Filter by outcome (Success or Failure) (optional).</param>
    /// <param name="startDate">Filter by start date (ISO 8601 format) (optional).</param>
    /// <param name="endDate">Filter by end date (ISO 8601 format) (optional).</param>
    /// <param name="page">The page number (1-indexed). Defaults to 1.</param>
    /// <param name="pageSize">The number of items per page. Defaults to 50, max 200.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated list of audit events matching the filters.</returns>
    /// <response code="200">Audit log retrieved successfully.</response>
    /// <response code="400">Invalid filter parameters or pagination.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="403">Non-admin user attempted access.</response>
    [HttpGet("audit-log")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] string? eventType = null,
        [FromQuery] Guid? actingUserId = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] Guid? resourceId = null,
        [FromQuery] string? outcome = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1)
        {
            return BadRequest(new { error = "Page must be 1 or greater." });
        }

        if (pageSize < 1 || pageSize > 200)
        {
            return BadRequest(new { error = "Page size must be between 1 and 200." });
        }

        // Validate outcome filter if provided
        AuditOutcome? parsedOutcome = null;
        if (!string.IsNullOrWhiteSpace(outcome))
        {
            if (!Enum.TryParse<AuditOutcome>(outcome, ignoreCase: true, out var parsedOutcomeValue))
            {
                return BadRequest(new { error = $"Invalid outcome. Must be one of: {string.Join(", ", Enum.GetNames(typeof(AuditOutcome)))}" });
            }

            parsedOutcome = parsedOutcomeValue;
        }

        // Validate date filters if provided
        DateTimeOffset? parsedStartDate = null;
        DateTimeOffset? parsedEndDate = null;

        if (!string.IsNullOrWhiteSpace(startDate))
        {
            if (!DateTimeOffset.TryParse(startDate, out var parsed))
            {
                return BadRequest(new { error = "Invalid startDate format. Use ISO 8601 format (e.g., 2024-01-01T00:00:00Z)." });
            }

            parsedStartDate = parsed;
        }

        if (!string.IsNullOrWhiteSpace(endDate))
        {
            if (!DateTimeOffset.TryParse(endDate, out var parsed))
            {
                return BadRequest(new { error = "Invalid endDate format. Use ISO 8601 format (e.g., 2024-01-31T23:59:59Z)." });
            }

            parsedEndDate = parsed;
        }

        if (parsedStartDate.HasValue && parsedEndDate.HasValue && parsedStartDate > parsedEndDate)
        {
            return BadRequest(new { error = "startDate must be before endDate." });
        }

        try
        {
            var (auditEvents, totalCount) = await _auditService.GetAuditLogAsync(
                eventType,
                actingUserId,
                resourceType,
                resourceId,
                parsedOutcome,
                parsedStartDate,
                parsedEndDate,
                page,
                pageSize,
                ct);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            _logger.LogInformation(
                "Admin retrieved audit log: page {Page}, pageSize {PageSize}, filters: eventType={EventType}, actingUserId={ActingUserId}, resourceType={ResourceType}",
                page, pageSize, eventType, actingUserId, resourceType);

            return Ok(new
            {
                auditEvents,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages
                }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid audit log filter parameters: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reassigns ownership of a resource (Location or LocationCollection) to a new owner.
    /// </summary>
    /// <param name="type">The resource type: "Location" or "LocationCollection".</param>
    /// <param name="id">The resource ID.</param>
    /// <param name="request">The reassignment request containing the new owner ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated resource.</returns>
    /// <response code="200">Ownership reassigned successfully.</response>
    /// <response code="400">Invalid resource type or validation error.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="403">Non-admin user attempted access.</response>
    /// <response code="404">Resource or new owner not found.</response>
    [HttpPost("resources/{type}/{id}/reassign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReassignResourceOwnership(
        [FromRoute] string type,
        [FromRoute] Guid id,
        [FromBody] ReassignResourceRequest request,
        CancellationToken ct)
    {
        if (request == null || request.NewOwnerId == Guid.Empty)
        {
            return BadRequest(new { error = "NewOwnerId is required and cannot be empty." });
        }

        var actingUserId = GetAuthenticatedUserId();
        if (actingUserId == Guid.Empty)
        {
            _logger.LogWarning("ReassignResourceOwnership called without valid user ID in claims");
            return Unauthorized(new { error = "Invalid authentication token." });
        }

        try
        {
            var updatedResource = await _userAdminService.ReassignResourceOwnershipAsync(
                type,
                id,
                request.NewOwnerId,
                actingUserId,
                ct);

            _logger.LogInformation(
                "Admin {ActingUserId} reassigned {ResourceType} {ResourceId} ownership to {NewOwnerId}",
                actingUserId, type, id, request.NewOwnerId);

            return Ok(updatedResource);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid reassignment parameters: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Reassignment failed - resource or owner not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports all application data as an AES-256 encrypted ZIP archive.
    /// </summary>
    /// <param name="request">The export request containing the encryption key (min 32 chars).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The encrypted backup archive as an octet-stream download.</returns>
    /// <response code="200">Export successful; returns encrypted ZIP file.</response>
    /// <response code="400">Encryption key is missing or shorter than 32 characters.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="403">Non-admin user attempted access.</response>
    [HttpPost("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(
        [FromBody] ExportRequest request,
        CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.EncryptionKey))
        {
            return BadRequest(new { error = "EncryptionKey is required." });
        }

        if (request.EncryptionKey.Length < 32)
        {
            return BadRequest(new { error = "EncryptionKey must be at least 32 characters." });
        }

        try
        {
            var stream = await _backupService.ExportAsync(request.EncryptionKey, ct);
            var fileName = $"backup-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.enc.zip";

            _logger.LogInformation("Admin initiated backup export");

            return File(stream, MediaTypeNames.Application.Octet, fileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Export failed due to invalid argument: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Imports application data from an AES-256 encrypted ZIP archive.
    /// </summary>
    /// <param name="file">The encrypted backup archive file.</param>
    /// <param name="decryptionKey">The decryption key used to encrypt the archive (min 32 chars).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ImportResult"/> summary of the import operation.</returns>
    /// <response code="200">Import successful; returns summary of imported records.</response>
    /// <response code="400">Decryption key is missing or shorter than 32 characters.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="403">Non-admin user attempted access.</response>
    /// <response code="422">Archive schema validation failed.</response>
    [HttpPost("import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Import(
        IFormFile file,
        [FromForm] string decryptionKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(decryptionKey))
        {
            return BadRequest(new { error = "DecryptionKey is required." });
        }

        if (decryptionKey.Length < 32)
        {
            return BadRequest(new { error = "DecryptionKey must be at least 32 characters." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "A backup file is required." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _backupService.ImportAsync(decryptionKey, stream, ct);

            _logger.LogInformation(
                "Admin completed backup import: {UsersImported} users, {LocationsImported} locations",
                result.UsersImported,
                result.LocationsImported);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Import failed due to invalid argument: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Import failed due to invalid archive: {Message}", ex.Message);
            return UnprocessableEntity(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request DTO for changing a user's role.
/// </summary>
public sealed record ChangeUserRoleRequest(
    string Role
);

/// <summary>
/// Request DTO for initiating a backup export.
/// </summary>
public sealed record ExportRequest(
    string EncryptionKey
);

/// <summary>
/// Request DTO for reassigning resource ownership.
/// </summary>
public sealed record ReassignResourceRequest(
    Guid NewOwnerId
);
