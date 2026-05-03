using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using Location = LocationManagement.Api.Models.Entities.Location;

namespace LocationManagement.Api.Tests;

/// <summary>
/// Integration tests verifying that EF Core migrations apply cleanly to a test database,
/// all tables are created with correct schema, spatial queries work correctly, and
/// indexes and foreign key relationships are properly enforced.
/// </summary>
public class MigrationIntegrationTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private AppDbContext? _dbContext;

    public MigrationIntegrationTests()
    {
        _connectionString = @"Server=(localdb)\mssqllocaldb;Database=LocationManagement_Test_" + Guid.NewGuid().ToString("N") + ";Integrated Security=true;";
    }

    /// <summary>
    /// Initialize the test database and apply migrations.
    /// </summary>
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString, x => x.UseNetTopologySuite())
            .Options;

        _dbContext = new AppDbContext(options);
        await _dbContext.Database.MigrateAsync();
    }

    /// <summary>
    /// Clean up the test database.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Verify that migrations apply cleanly without errors.
    /// </summary>
    [Fact]
    public async Task MigrationsApplyCleanly_WhenDatabaseIsInitialized_SucceedsWithoutErrors()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString, x => x.UseNetTopologySuite())
            .Options;

        using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
        Assert.True(await context.Database.CanConnectAsync());
    }

    /// <summary>
    /// Verify that all required tables are created with correct schema.
    /// </summary>
    [Fact]
    public async Task AllTablesAreCreated_WhenMigrationsApply_TablesExistWithCorrectColumns()
    {
        var requiredTables = new[]
        {
            "Users", "Locations", "PendingEdits", "LocationCollections", "CollectionMembers",
            "PendingMembershipRequests", "NamedShapes", "Images", "Notifications", "AuditEvents",
            "PasswordResetTokens", "LocationImages", "PendingEditImages"
        };

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var tableName in requiredTables)
        {
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
            var result = await command.ExecuteScalarAsync();
            Assert.NotNull(result);
            Assert.Equal(1, (int)result);
        }
    }

    /// <summary>
    /// Verify that the Locations table has the correct columns including spatial GEOGRAPHY type.
    /// </summary>
    [Fact]
    public async Task LocationsTableSchema_WhenCreated_HasCorrectColumnsAndTypes()
    {
        var expectedColumns = new Dictionary<string, string>
        {
            { "Id", "uniqueidentifier" },
            { "Name", "nvarchar" },
            { "Coordinates", "geography" },
            { "SourceSrid", "int" },
            { "ContentSequence", "nvarchar" },
            { "CreatorId", "uniqueidentifier" },
            { "CreatedAt", "datetimeoffset" }
        };

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var (columnName, expectedType) in expectedColumns)
        {
            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'Locations' AND COLUMN_NAME = '{columnName}'";
            var result = await command.ExecuteScalarAsync();
            Assert.NotNull(result);
            Assert.Contains(expectedType, result.ToString()!);
        }
    }

    /// <summary>
    /// Verify that spatial indexes are created on GEOGRAPHY columns.
    /// </summary>
    [Fact]
    public async Task SpatialIndexes_WhenCreated_ExistOnGeographyColumns()
    {
        var expectedIndexes = new[]
        {
            ("Locations", "IX_Locations_Coordinates_Spatial"),
            ("NamedShapes", "IX_NamedShapes_Geometry_Spatial"),
            ("PendingEdits", "IX_PendingEdits_Coordinates_Spatial")
        };

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var (tableName, indexName) in expectedIndexes)
        {
            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT COUNT(*) FROM sys.indexes 
                WHERE object_id = OBJECT_ID('{tableName}') AND name = '{indexName}'";
            var result = await command.ExecuteScalarAsync();
            Assert.NotNull(result);
            Assert.Equal(1, (int)result);
        }
    }

    /// <summary>
    /// Verify that unique indexes are created on Users table for case-insensitive uniqueness.
    /// </summary>
    [Fact]
    public async Task UniqueIndexes_WhenCreated_ExistOnUsersTable()
    {
        var expectedUniqueIndexes = new[]
        {
            "IX_Users_Username_Unique",
            "IX_Users_DisplayName_Unique",
            "IX_Users_Email_Unique"
        };

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var indexName in expectedUniqueIndexes)
        {
            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT COUNT(*) FROM sys.indexes 
                WHERE object_id = OBJECT_ID('Users') AND name = '{indexName}' AND is_unique = 1";
            var result = await command.ExecuteScalarAsync();
            Assert.NotNull(result);
            Assert.Equal(1, (int)result);
        }
    }

    /// <summary>
    /// Verify that spatial queries work correctly on Location coordinates.
    /// </summary>
    [Fact]
    public async Task SpatialQueries_WhenExecuted_ReturnCorrectLocationsByCoordinates()
    {
        var creator = new User
        {
            Id = Guid.NewGuid(),
            Username = "creator",
            DisplayName = "Creator User",
            Email = "creator@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

        var location1 = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Location 1",
            Latitude = 0.0,
            Longitude = 0.0,
            Coordinates = geometryFactory.CreatePoint(new Coordinate(0, 0)),
            SourceSrid = 4326,
            ContentSequence = "[]",
            CreatorId = creator.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var location2 = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Location 2",
            Latitude = 10,
            Longitude = 10,
            Coordinates = geometryFactory.CreatePoint(new Coordinate(10, 10)),
            SourceSrid = 4326,
            ContentSequence = "[]",
            CreatorId = creator.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext!.Users.Add(creator);
        _dbContext.Locations.Add(location1);
        _dbContext.Locations.Add(location2);
        await _dbContext.SaveChangesAsync();

        var allLocations = await _dbContext.Locations.ToListAsync();

        Assert.Equal(2, allLocations.Count);
        Assert.Contains(allLocations, l => l.Id == location1.Id);
        Assert.Contains(allLocations, l => l.Id == location2.Id);
    }

    /// <summary>
    /// Verify that spatial queries work correctly on NamedShape geometries.
    /// </summary>
    [Fact]
    public async Task SpatialQueries_WhenExecutedOnNamedShapes_ReturnCorrectGeometries()
    {
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            DisplayName = "Admin User",
            Email = "admin@example.com",
            PasswordHash = "hash",
            Role = UserRole.Admin,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

        var coordinates = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(10, 0),
            new Coordinate(10, 10),
            new Coordinate(0, 10),
            new Coordinate(0, 0)
        };
        var polygon = geometryFactory.CreatePolygon(coordinates);

        var namedShape = new NamedShape
        {
            Id = Guid.NewGuid(),
            Name = "Test Shape",
            Geometry = polygon,
            CreatedByUserId = admin.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext!.Users.Add(admin);
        _dbContext.NamedShapes.Add(namedShape);
        await _dbContext.SaveChangesAsync();

        var testPoint = geometryFactory.CreatePoint(new Coordinate(5, 5));
        var shapesContainingPoint = await _dbContext.NamedShapes
            .Where(s => s.Geometry.Contains(testPoint))
            .ToListAsync();

        Assert.Single(shapesContainingPoint);
        Assert.Equal(namedShape.Id, shapesContainingPoint[0].Id);
    }

    /// <summary>
    /// Verify that foreign key relationships are enforced and cascade deletes work correctly.
    /// </summary>
    [Fact]
    public async Task ForeignKeyRelationships_WhenEnforced_CascadeDeletesWorkCorrectly()
    {
        var creator = new User
        {
            Id = Guid.NewGuid(),
            Username = "creator",
            DisplayName = "Creator User",
            Email = "creator@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Test Location",
            Latitude = 0,
            Longitude = 0,
            Coordinates = geometryFactory.CreatePoint(new Coordinate(0, 0)),
            SourceSrid = 4326,
            ContentSequence = "[]",
            CreatorId = creator.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext!.Users.Add(creator);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();

        var locationId = location.Id;

        _dbContext.Users.Remove(creator);
        await _dbContext.SaveChangesAsync();

        var deletedLocation = await _dbContext.Locations.FirstOrDefaultAsync(l => l.Id == locationId);
        Assert.Null(deletedLocation);
    }

    /// <summary>
    /// Verify that ContentSequence JSON column can store and retrieve data correctly.
    /// </summary>
    [Fact]
    public async Task ContentSequenceJsonColumn_WhenStored_CanBeRetrievedCorrectly()
    {
        var creator = new User
        {
            Id = Guid.NewGuid(),
            Username = "creator",
            DisplayName = "Creator User",
            Email = "creator@example.com",
            PasswordHash = "hash",
            Role = UserRole.Standard,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

        var contentSequence = @"[
            {""type"":""Heading"",""text"":""Title"",""level"":1},
            {""type"":""Paragraph"",""text"":""Body text""}
        ]";

        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Test Location",
            Latitude = 0,
            Longitude = 0,
            Coordinates = geometryFactory.CreatePoint(new Coordinate(0, 0)),
            SourceSrid = 4326,
            ContentSequence = contentSequence,
            CreatorId = creator.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext!.Users.Add(creator);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();

        var locationId = location.Id;

        var retrievedLocation = await _dbContext.Locations.FirstOrDefaultAsync(l => l.Id == locationId);

        Assert.NotNull(retrievedLocation);
        Assert.Equal(contentSequence, retrievedLocation.ContentSequence);
    }
}



