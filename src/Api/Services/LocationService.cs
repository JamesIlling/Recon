using System.Text.Json;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using LocationEntity = LocationManagement.Api.Models.Entities.Location;

namespace LocationManagement.Api.Services;

/// <summary>
/// Implements location management operations including creation, retrieval, editing,
/// and the PendingEdit approval workflow.
/// </summary>
public sealed class LocationService : ILocationService
{
    private const int Wgs84Srid = 4326;

    private readonly AppDbContext _dbContext;
    private readonly ILocationRepository _locationRepository;
    private readonly IPendingEditRepository _pendingEditRepository;
    private readonly ICoordinateReprojectionService _coordinateReprojectionService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<LocationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationService"/> class.
    /// </summary>
    public LocationService(
        AppDbContext dbContext,
        ILocationRepository locationRepository,
        IPendingEditRepository pendingEditRepository,
        ICoordinateReprojectionService coordinateReprojectionService,
        IImageProcessingService imageProcessingService,
        IAuditService auditService,
        INotificationService notificationService,
        ILogger<LocationService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
        _pendingEditRepository = pendingEditRepository ?? throw new ArgumentNullException(nameof(pendingEditRepository));
        _coordinateReprojectionService = coordinateReprojectionService ?? throw new ArgumentNullException(nameof(coordinateReprojectionService));
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<LocationEntity> CreateAsync(
        string name,
        double latitude,
        double longitude,
        int sourceSrid,
        string contentSequenceJson,
        Guid creatorId,
        string sourceIp,
        CancellationToken ct)
    {
        ValidateCoordinates(latitude, longitude);
        ValidateSrid(sourceSrid);

        var (lat, lon) = _coordinateReprojectionService.ReprojectToWgs84(latitude, longitude, sourceSrid);
        var location = BuildLocation(name, lat, lon, sourceSrid, contentSequenceJson, creatorId);
        var created = await _locationRepository.CreateAsync(location, ct).ConfigureAwait(false);

        _logger.LogInformation("Location created: {LocationId} by user {UserId}", created.Id, creatorId);
        await _auditService.RecordAsync("LocationCreated", creatorId, "Location", created.Id, AuditOutcome.Success, sourceIp, ct).ConfigureAwait(false);

        return created;
    }

    /// <inheritdoc />
    public async Task<LocationEntity?> GetByIdAsync(Guid id, Guid? requestingUserId, CancellationToken ct)
    {
        var location = await _locationRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (location == null)
            return null;

        if (requestingUserId.HasValue)
            await OverlayPendingEditIfPresent(location, id, requestingUserId.Value, ct).ConfigureAwait(false);

        return location;
    }

    /// <inheritdoc />
    public async Task<(int TotalCount, List<LocationEntity> Items)> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page < 1)
            throw new ArgumentException("Page must be >= 1.", nameof(page));
        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("PageSize must be between 1 and 100.", nameof(pageSize));

        return await _locationRepository.ListAsync(page, pageSize, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LocationEntity> UpdateAsync(
        Guid id,
        string name,
        double latitude,
        double longitude,
        int sourceSrid,
        string contentSequenceJson,
        Guid userId,
        string sourceIp,
        CancellationToken ct)
    {
        ValidateCoordinates(latitude, longitude);
        ValidateSrid(sourceSrid);

        var location = await RequireLocationAsync(id, ct).ConfigureAwait(false);
        RequireCreator(location, userId);

        var oldSeq = location.ContentSequence;
        var (lat, lon) = _coordinateReprojectionService.ReprojectToWgs84(latitude, longitude, sourceSrid);
        ApplyLocationChanges(location, name, lat, lon, sourceSrid, contentSequenceJson);

        var updated = await _locationRepository.UpdateAsync(location, ct).ConfigureAwait(false);
        await CleanupOrphanedImagesAsync(oldSeq, contentSequenceJson, ct).ConfigureAwait(false);

        _logger.LogInformation("Location updated: {LocationId} by user {UserId}", id, userId);
        await _auditService.RecordAsync("LocationUpdated", userId, "Location", id, AuditOutcome.Success, sourceIp, ct).ConfigureAwait(false);

        return updated;
    }

    /// <inheritdoc />
    public async Task<PendingEdit> SubmitPendingEditAsync(
        Guid id,
        string name,
        double latitude,
        double longitude,
        int sourceSrid,
        string contentSequenceJson,
        Guid submittedByUserId,
        string sourceIp,
        CancellationToken ct)
    {
        ValidateCoordinates(latitude, longitude);
        ValidateSrid(sourceSrid);

        var location = await RequireLocationAsync(id, ct).ConfigureAwait(false);
        if (location.CreatorId == submittedByUserId)
            throw new InvalidOperationException("Creator must use the direct update path.");

        var (lat, lon) = _coordinateReprojectionService.ReprojectToWgs84(latitude, longitude, sourceSrid);
        var pe = await UpsertPendingEditAsync(id, name, lat, lon, sourceSrid, contentSequenceJson, submittedByUserId, location, ct).ConfigureAwait(false);

        _logger.LogInformation("PendingEdit submitted: {EditId} for location {LocationId}", pe.Id, id);
        await _auditService.RecordAsync("PendingEditSubmitted", submittedByUserId, "PendingEdit", pe.Id, AuditOutcome.Success, sourceIp, ct).ConfigureAwait(false);

        return pe;
    }

    /// <inheritdoc />
    public async Task<LocationEntity> ApprovePendingEditAsync(
        Guid locationId,
        Guid editId,
        Guid userId,
        string sourceIp,
        CancellationToken ct)
    {
        var location = await RequireLocationAsync(locationId, ct).ConfigureAwait(false);
        RequireCreator(location, userId);

        var pe = await RequirePendingEditAsync(editId, locationId, ct).ConfigureAwait(false);
        var oldSeq = location.ContentSequence;

        ApplyPendingEditToLocation(location, pe);
        var updated = await _locationRepository.UpdateAsync(location, ct).ConfigureAwait(false);

        await _pendingEditRepository.DeleteAsync(editId, ct).ConfigureAwait(false);
        await CleanupOrphanedImagesAsync(oldSeq, pe.ContentSequence, ct).ConfigureAwait(false);
        await _notificationService.NotifyEditApprovedAsync(pe.SubmittedByUserId, locationId, location.Name, ct).ConfigureAwait(false);

        _logger.LogInformation("PendingEdit approved: {EditId} for location {LocationId}", editId, locationId);
        await _auditService.RecordAsync("PendingEditApproved", userId, "Location", locationId, AuditOutcome.Success, sourceIp, ct).ConfigureAwait(false);

        return updated;
    }

    /// <inheritdoc />
    public async Task RejectPendingEditAsync(
        Guid locationId,
        Guid editId,
        Guid userId,
        string sourceIp,
        CancellationToken ct)
    {
        var location = await RequireLocationAsync(locationId, ct).ConfigureAwait(false);
        RequireCreator(location, userId);

        var pe = await RequirePendingEditAsync(editId, locationId, ct).ConfigureAwait(false);

        await CleanupOrphanedImagesAsync(pe.ContentSequence, location.ContentSequence, ct).ConfigureAwait(false);
        await _pendingEditRepository.DeleteAsync(editId, ct).ConfigureAwait(false);
        await _notificationService.NotifyEditRejectedAsync(pe.SubmittedByUserId, locationId, location.Name, ct).ConfigureAwait(false);

        _logger.LogInformation("PendingEdit rejected: {EditId} for location {LocationId}", editId, locationId);
        await _auditService.RecordAsync("PendingEditRejected", userId, "Location", locationId, AuditOutcome.Success, sourceIp, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, Guid userId, bool isAdmin, string sourceIp, CancellationToken ct)
    {
        var location = await RequireLocationAsync(id, ct).ConfigureAwait(false);
        if (location.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Only the creator or an admin may delete a location.");

        await DeletePendingEditsForLocationAsync(id, ct).ConfigureAwait(false);
        await DeleteCollectionMembersForLocationAsync(id, ct).ConfigureAwait(false);
        await CleanupOrphanedImagesAsync(location.ContentSequence, string.Empty, ct).ConfigureAwait(false);
        await _locationRepository.DeleteAsync(id, ct).ConfigureAwait(false);

        _logger.LogInformation("Location deleted: {LocationId} by user {UserId}", id, userId);
        await _auditService.RecordAsync("LocationDeleted", userId, "Location", id, AuditOutcome.Success, sourceIp, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<PendingEdit>> GetPendingEditsAsync(Guid locationId, Guid userId, CancellationToken ct)
    {
        var location = await RequireLocationAsync(locationId, ct).ConfigureAwait(false);
        RequireCreator(location, userId);

        return await _pendingEditRepository.GetByLocationIdAsync(locationId, ct).ConfigureAwait(false);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90.", nameof(latitude));
        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180.", nameof(longitude));
    }

    private void ValidateSrid(int sourceSrid)
    {
        if (!_coordinateReprojectionService.IsSridSupported(sourceSrid))
            throw new ArgumentException($"SRID {sourceSrid} is not supported for reprojection to WGS84.", nameof(sourceSrid));
    }

    private static void RequireCreator(LocationEntity location, Guid userId)
    {
        if (location.CreatorId != userId)
            throw new UnauthorizedAccessException("Only the location creator may perform this operation.");
    }

    private async Task<LocationEntity> RequireLocationAsync(Guid id, CancellationToken ct)
    {
        return await _locationRepository.GetByIdAsync(id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Location {id} not found.");
    }

    private async Task<PendingEdit> RequirePendingEditAsync(Guid editId, Guid locationId, CancellationToken ct)
    {
        var pe = await _pendingEditRepository.GetByIdAsync(editId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"PendingEdit {editId} not found.");

        if (pe.LocationId != locationId)
            throw new InvalidOperationException("PendingEdit does not belong to the specified location.");

        return pe;
    }

    private static LocationEntity BuildLocation(
        string name,
        double lat,
        double lon,
        int sourceSrid,
        string contentSequenceJson,
        Guid creatorId)
    {
        var gf = new GeometryFactory(new PrecisionModel(), Wgs84Srid);
        return new LocationEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Latitude = lat,
            Longitude = lon,
            Coordinates = gf.CreatePoint(new Coordinate(lon, lat)),
            SourceSrid = sourceSrid,
            ContentSequence = contentSequenceJson,
            CreatorId = creatorId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void ApplyLocationChanges(
        LocationEntity location,
        string name,
        double lat,
        double lon,
        int sourceSrid,
        string contentSequenceJson)
    {
        var gf = new GeometryFactory(new PrecisionModel(), Wgs84Srid);
        location.Name = name;
        location.Latitude = lat;
        location.Longitude = lon;
        location.Coordinates = gf.CreatePoint(new Coordinate(lon, lat));
        location.SourceSrid = sourceSrid;
        location.ContentSequence = contentSequenceJson;
        location.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ApplyPendingEditToLocation(LocationEntity location, PendingEdit pe)
    {
        location.Name = pe.Name;
        location.Latitude = pe.Latitude;
        location.Longitude = pe.Longitude;
        location.Coordinates = pe.Coordinates;
        location.SourceSrid = pe.SourceSrid;
        location.ContentSequence = pe.ContentSequence;
        location.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task OverlayPendingEditIfPresent(
        LocationEntity location,
        Guid locationId,
        Guid userId,
        CancellationToken ct)
    {
        var pe = await _pendingEditRepository
            .GetByLocationAndUserAsync(locationId, userId, ct)
            .ConfigureAwait(false);

        if (pe != null)
            ApplyPendingEditToLocation(location, pe);
    }

    private async Task<PendingEdit> UpsertPendingEditAsync(
        Guid locationId,
        string name,
        double lat,
        double lon,
        int sourceSrid,
        string contentSequenceJson,
        Guid submittedByUserId,
        LocationEntity location,
        CancellationToken ct)
    {
        var existing = await _pendingEditRepository
            .GetByLocationAndUserAsync(locationId, submittedByUserId, ct)
            .ConfigureAwait(false);

        if (existing != null)
            return await UpdateExistingPendingEditAsync(existing, name, lat, lon, sourceSrid, contentSequenceJson, ct).ConfigureAwait(false);

        return await CreateNewPendingEditAsync(locationId, name, lat, lon, sourceSrid, contentSequenceJson, submittedByUserId, location, ct).ConfigureAwait(false);
    }

    private async Task<PendingEdit> UpdateExistingPendingEditAsync(
        PendingEdit existing,
        string name,
        double lat,
        double lon,
        int sourceSrid,
        string contentSequenceJson,
        CancellationToken ct)
    {
        var gf = new GeometryFactory(new PrecisionModel(), Wgs84Srid);
        existing.Name = name;
        existing.Latitude = lat;
        existing.Longitude = lon;
        existing.Coordinates = gf.CreatePoint(new Coordinate(lon, lat));
        existing.SourceSrid = sourceSrid;
        existing.ContentSequence = contentSequenceJson;
        return await _pendingEditRepository.UpdateAsync(existing, ct).ConfigureAwait(false);
    }

    private async Task<PendingEdit> CreateNewPendingEditAsync(
        Guid locationId,
        string name,
        double lat,
        double lon,
        int sourceSrid,
        string contentSequenceJson,
        Guid submittedByUserId,
        LocationEntity location,
        CancellationToken ct)
    {
        var gf = new GeometryFactory(new PrecisionModel(), Wgs84Srid);
        var pe = new PendingEdit
        {
            Id = Guid.NewGuid(),
            LocationId = locationId,
            SubmittedByUserId = submittedByUserId,
            Name = name,
            Latitude = lat,
            Longitude = lon,
            Coordinates = gf.CreatePoint(new Coordinate(lon, lat)),
            SourceSrid = sourceSrid,
            ContentSequence = contentSequenceJson,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        var created = await _pendingEditRepository.CreateAsync(pe, ct).ConfigureAwait(false);
        await _notificationService
            .NotifyPendingEditSubmittedAsync(location.CreatorId, locationId, location.Name, ct)
            .ConfigureAwait(false);

        return created;
    }

    private async Task DeletePendingEditsForLocationAsync(Guid locationId, CancellationToken ct)
    {
        var pendingEdits = await _pendingEditRepository
            .GetByLocationIdAsync(locationId, ct)
            .ConfigureAwait(false);

        foreach (var pe in pendingEdits)
        {
            await CleanupOrphanedImagesAsync(pe.ContentSequence, string.Empty, ct).ConfigureAwait(false);
            await _pendingEditRepository.DeleteAsync(pe.Id, ct).ConfigureAwait(false);
        }
    }

    private async Task DeleteCollectionMembersForLocationAsync(Guid locationId, CancellationToken ct)
    {
        var members = await _dbContext.CollectionMembers
            .Where(cm => cm.LocationId == locationId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _dbContext.CollectionMembers.RemoveRange(members);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task CleanupOrphanedImagesAsync(string oldSeq, string newSeq, CancellationToken ct)
    {
        var oldIds = ExtractImageIds(oldSeq);
        var newIds = ExtractImageIds(newSeq);
        var orphaned = oldIds.Except(newIds).ToList();

        foreach (var imageId in orphaned)
        {
            var isReferenced = await IsImageReferencedAsync(imageId, ct).ConfigureAwait(false);
            if (!isReferenced)
                await _imageProcessingService.DeleteImageAndVariantsAsync(imageId, ct).ConfigureAwait(false);
        }
    }

    private async Task<bool> IsImageReferencedAsync(Guid imageId, CancellationToken ct)
    {
        var idStr = imageId.ToString();
        var inLocations = await _dbContext.Locations
            .AnyAsync(l => l.ContentSequence.Contains(idStr), ct)
            .ConfigureAwait(false);

        if (inLocations)
            return true;

        return await _dbContext.PendingEdits
            .AnyAsync(pe => pe.ContentSequence.Contains(idStr), ct)
            .ConfigureAwait(false);
    }

    private static List<Guid> ExtractImageIds(string json)
    {
        var ids = new List<Guid>();
        if (string.IsNullOrWhiteSpace(json))
            return ids;

        try
        {
            var blocks = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (blocks == null)
                return ids;

            foreach (var block in blocks)
            {
                if (block.TryGetProperty("type", out var typeEl)
                    && typeEl.GetString() == "Image"
                    && block.TryGetProperty("imageId", out var idEl)
                    && Guid.TryParse(idEl.GetString(), out var id))
                {
                    ids.Add(id);
                }
            }
        }
        catch (JsonException)
        {
            // Malformed JSON — return empty list; validation happens at the API boundary.
        }

        return ids;
    }
}
