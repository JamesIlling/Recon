using System.Text.Json;
using NetTopologySuite.Geometries;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Repositories;

namespace LocationManagement.Api.Services;

/// <summary>
/// Service implementation for NamedShape operations.
/// Handles upload, rename, delete, and list operations with validation and audit logging.
/// </summary>
public sealed class NamedShapeService : INamedShapeService
{
    private const int MaxNameLength = 200;
    private const int MaxVertexCount = 1000;
    private const string GeometryBombErrorMessage = "Geometry exceeds maximum vertex count of 1000";

    private readonly INamedShapeRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<NamedShapeService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedShapeService"/> class.
    /// </summary>
    /// <param name="repository">The NamedShape repository.</param>
    /// <param name="auditService">The audit service for recording events.</param>
    /// <param name="logger">The logger.</param>
    public NamedShapeService(
        INamedShapeRepository repository,
        IAuditService auditService,
        ILogger<NamedShapeService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Uploads a new NamedShape from GeoJSON geometry.
    /// </summary>
    public async Task<NamedShape> UploadAsync(string name, string geoJsonGeometry, Guid adminUserId, string sourceIp, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(geoJsonGeometry);

        // Validate name length
        if (name.Length > MaxNameLength)
            throw new ArgumentException($"Name exceeds maximum length of {MaxNameLength} characters.", nameof(name));

        // Check name uniqueness
        var existingShape = await _repository.GetByNameAsync(name, cancellationToken);
        if (existingShape != null)
            throw new InvalidOperationException($"A NamedShape with name '{name}' already exists.");

        // Parse and validate GeoJSON geometry
        Geometry geometry;
        try
        {
            // Parse GeoJSON string to validate format
            using var doc = JsonDocument.Parse(geoJsonGeometry);
            var root = doc.RootElement;

            // Validate it's a valid GeoJSON object with type and coordinates
            if (!root.TryGetProperty("type", out var typeElement))
                throw new ArgumentException("GeoJSON must contain a 'type' property.");

            var geometryType = typeElement.GetString();
            if (geometryType is not ("Polygon" or "MultiPolygon"))
                throw new ArgumentException("Geometry must be a Polygon or MultiPolygon.");

            // For now, create a simple geometry from the GeoJSON
            // In production, you'd use a proper GeoJSON parser
            var factory = new GeometryFactory(new PrecisionModel(), 4326);

            // Parse coordinates array
            if (!root.TryGetProperty("coordinates", out var coordsElement))
                throw new ArgumentException("GeoJSON must contain a 'coordinates' property.");

            // Create a simple polygon from the first coordinate ring
            var coordsArray = coordsElement.EnumerateArray().ToList();
            if (coordsArray.Count == 0)
                throw new ArgumentException("Coordinates array cannot be empty.");

            var firstRing = coordsArray[0];
            var coords = new List<Coordinate>();
            var vertexCount = 0;

            foreach (var coordPair in firstRing.EnumerateArray())
            {
                var pair = coordPair.EnumerateArray().ToList();
                if (pair.Count < 2)
                    throw new ArgumentException("Each coordinate must have at least 2 values.");

                var lon = pair[0].GetDouble();
                var lat = pair[1].GetDouble();
                coords.Add(new Coordinate(lon, lat));
                vertexCount++;
            }

            // Validate vertex count (geometry bomb protection)
            if (vertexCount > MaxVertexCount)
            {
                _logger.LogWarning("Geometry bomb detected: {VertexCount} vertices exceeds maximum of {MaxVertexCount}", vertexCount, MaxVertexCount);
                throw new ArgumentException(GeometryBombErrorMessage, nameof(geoJsonGeometry));
            }

            // Create geometry
            if (geometryType == "Polygon")
            {
                var shell = factory.CreateLinearRing(coords.ToArray());
                geometry = factory.CreatePolygon(shell);
            }
            else // MultiPolygon
            {
                var polygons = new List<Polygon>();
                foreach (var ring in coordsArray)
                {
                    var ringCoords = new List<Coordinate>();
                    foreach (var coordPair in ring.EnumerateArray())
                    {
                        var pair = coordPair.EnumerateArray().ToList();
                        if (pair.Count < 2)
                            throw new ArgumentException("Each coordinate must have at least 2 values.");

                        var lon = pair[0].GetDouble();
                        var lat = pair[1].GetDouble();
                        ringCoords.Add(new Coordinate(lon, lat));
                    }
                    var shell = factory.CreateLinearRing(ringCoords.ToArray());
                    polygons.Add(factory.CreatePolygon(shell));
                }
                geometry = factory.CreateMultiPolygon(polygons.ToArray());
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Invalid GeoJSON geometry provided: {Exception}", ex.Message);
            throw new ArgumentException("Invalid GeoJSON geometry format.", nameof(geoJsonGeometry), ex);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error parsing GeoJSON geometry: {Exception}", ex.Message);
            throw new ArgumentException("Invalid GeoJSON geometry format.", nameof(geoJsonGeometry), ex);
        }

        // Create NamedShape entity
        var namedShape = new NamedShape
        {
            Id = Guid.NewGuid(),
            Name = name,
            Geometry = geometry,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = adminUserId
        };

        // Persist to database
        var created = await _repository.CreateAsync(namedShape, cancellationToken);

        // Record audit event
        await _auditService.RecordAsync(
            "NamedShapeUploaded",
            adminUserId,
            "NamedShape",
            created.Id,
            AuditOutcome.Success,
            sourceIp,
            cancellationToken);

        _logger.LogInformation("NamedShape uploaded: {NamedShapeId} by {AdminUserId}", created.Id, adminUserId);

        return created;
    }

    /// <summary>
    /// Renames an existing NamedShape.
    /// </summary>
    public async Task<NamedShape> RenameAsync(Guid id, string newName, Guid adminUserId, string sourceIp, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        // Validate name length
        if (newName.Length > MaxNameLength)
            throw new ArgumentException($"Name exceeds maximum length of {MaxNameLength} characters.", nameof(newName));

        // Get existing shape
        var namedShape = await _repository.GetByIdAsync(id, cancellationToken);
        if (namedShape == null)
            throw new InvalidOperationException($"NamedShape with ID {id} not found.");

        // Check new name uniqueness (excluding current shape)
        var existingWithNewName = await _repository.GetByNameAsync(newName, cancellationToken);
        if (existingWithNewName != null && existingWithNewName.Id != id)
            throw new InvalidOperationException($"A NamedShape with name '{newName}' already exists.");

        // Update name
        namedShape.Name = newName;
        var updated = await _repository.UpdateAsync(namedShape, cancellationToken);

        // Record audit event
        await _auditService.RecordAsync(
            "NamedShapeRenamed",
            adminUserId,
            "NamedShape",
            updated.Id,
            AuditOutcome.Success,
            sourceIp,
            cancellationToken);

        _logger.LogInformation("NamedShape renamed: {NamedShapeId} to '{NewName}' by {AdminUserId}", updated.Id, newName, adminUserId);

        return updated;
    }

    /// <summary>
    /// Deletes a NamedShape.
    /// </summary>
    public async Task DeleteAsync(Guid id, Guid adminUserId, string sourceIp, CancellationToken cancellationToken)
    {
        // Get existing shape
        var namedShape = await _repository.GetByIdAsync(id, cancellationToken);
        if (namedShape == null)
            throw new InvalidOperationException($"NamedShape with ID {id} not found.");

        // Check if referenced by any collection
        var isReferenced = await _repository.IsReferencedByCollectionAsync(id, cancellationToken);
        if (isReferenced)
            throw new InvalidOperationException("Cannot delete NamedShape that is referenced by one or more LocationCollections.");

        // Delete from database
        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
            throw new InvalidOperationException($"Failed to delete NamedShape with ID {id}.");

        // Record audit event
        await _auditService.RecordAsync(
            "NamedShapeDeleted",
            adminUserId,
            "NamedShape",
            id,
            AuditOutcome.Success,
            sourceIp,
            cancellationToken);

        _logger.LogInformation("NamedShape deleted: {NamedShapeId} by {AdminUserId}", id, adminUserId);
    }

    /// <summary>
    /// Lists all NamedShapes with pagination.
    /// </summary>
    public async Task<(int TotalCount, List<(Guid Id, string Name)> Items)> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var (totalCount, items) = await _repository.ListAsync(page, pageSize, cancellationToken);

        // Project to id and name only
        var projectedItems = items
            .Select(ns => (ns.Id, ns.Name))
            .ToList();

        return (totalCount, projectedItems);
    }
}
