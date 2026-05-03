namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Represents the structured JSON manifest of all exportable data in a backup archive.
/// Contains all users, locations, collections, named shapes, and audit events.
/// </summary>
public class BackupManifest
{
    /// <summary>
    /// Gets or sets the version of the backup format (for future compatibility).
    /// </summary>
    public required int Version { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the backup was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the collection of exported users.
    /// </summary>
    public required List<BackupUser> Users { get; set; }

    /// <summary>
    /// Gets or sets the collection of exported locations.
    /// </summary>
    public required List<BackupLocation> Locations { get; set; }

    /// <summary>
    /// Gets or sets the collection of exported location collections.
    /// </summary>
    public required List<BackupLocationCollection> LocationCollections { get; set; }

    /// <summary>
    /// Gets or sets the collection of exported collection members (location-collection associations).
    /// </summary>
    public required List<BackupCollectionMember> CollectionMembers { get; set; }

    /// <summary>
    /// Gets or sets the collection of exported named shapes.
    /// </summary>
    public required List<BackupNamedShape> NamedShapes { get; set; }

    /// <summary>
    /// Gets or sets the collection of exported images (metadata only; files are in the ZIP).
    /// </summary>
    public required List<BackupImage> Images { get; set; }

    /// <summary>
    /// Gets or sets the collection of exported audit events.
    /// </summary>
    public required List<BackupAuditEvent> AuditEvents { get; set; }
}

/// <summary>
/// Represents a user record in the backup manifest.
/// </summary>
public class BackupUser
{
    /// <summary>Gets or sets the original user ID.</summary>
    public required Guid Id { get; set; }

    /// <summary>Gets or sets the username.</summary>
    public required string Username { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public required string DisplayName { get; set; }

    /// <summary>Gets or sets the email address.</summary>
    public required string Email { get; set; }

    /// <summary>Gets or sets the password hash (bcrypt).</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Gets or sets the user role.</summary>
    public required string Role { get; set; }

    /// <summary>Gets or sets the avatar image ID (if any).</summary>
    public Guid? AvatarImageId { get; set; }

    /// <summary>Gets or sets the ShowPublicCollections preference.</summary>
    public required bool ShowPublicCollections { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public required DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Represents a location record in the backup manifest.
/// </summary>
public class BackupLocation
{
    /// <summary>Gets or sets the original location ID.</summary>
    public required Guid Id { get; set; }

    /// <summary>Gets or sets the location name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the latitude (WGS84).</summary>
    public required double Latitude { get; set; }

    /// <summary>Gets or sets the longitude (WGS84).</summary>
    public required double Longitude { get; set; }

    /// <summary>Gets or sets the source SRID.</summary>
    public required int SourceSrid { get; set; }

    /// <summary>Gets or sets the content sequence JSON.</summary>
    public required string ContentSequence { get; set; }

    /// <summary>Gets or sets the original creator user ID.</summary>
    public required Guid CreatorId { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public required DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Represents a location collection record in the backup manifest.
/// </summary>
public class BackupLocationCollection
{
    /// <summary>Gets or sets the original collection ID.</summary>
    public required Guid Id { get; set; }

    /// <summary>Gets or sets the collection name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the original owner user ID.</summary>
    public required Guid OwnerId { get; set; }

    /// <summary>Gets or sets the thumbnail image ID (if any).</summary>
    public Guid? ThumbnailImageId { get; set; }

    /// <summary>Gets or sets the bounding shape ID (if any).</summary>
    public Guid? BoundingShapeId { get; set; }

    /// <summary>Gets or sets whether the collection is public.</summary>
    public required bool IsPublic { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public required DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Represents a collection member (location-collection association) in the backup manifest.
/// </summary>
public class BackupCollectionMember
{
    /// <summary>Gets or sets the original location ID.</summary>
    public required Guid LocationId { get; set; }

    /// <summary>Gets or sets the original collection ID.</summary>
    public required Guid CollectionId { get; set; }
}

/// <summary>
/// Represents a named shape record in the backup manifest.
/// </summary>
public class BackupNamedShape
{
    /// <summary>Gets or sets the original shape ID.</summary>
    public required Guid Id { get; set; }

    /// <summary>Gets or sets the shape name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the GeoJSON geometry as a string.</summary>
    public required string Geometry { get; set; }

    /// <summary>Gets or sets the original creator user ID.</summary>
    public required Guid CreatedByUserId { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public required DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Represents an image record in the backup manifest.
/// </summary>
public class BackupImage
{
    /// <summary>Gets or sets the original image ID.</summary>
    public required Guid Id { get; set; }

    /// <summary>Gets or sets the original filename.</summary>
    public required string FileName { get; set; }

    /// <summary>Gets or sets the MIME type.</summary>
    public required string MimeType { get; set; }

    /// <summary>Gets or sets the optional alt text.</summary>
    public string? AltText { get; set; }

    /// <summary>Gets or sets the file size in bytes.</summary>
    public required long FileSize { get; set; }

    /// <summary>Gets or sets the original uploader user ID.</summary>
    public required Guid UploadedByUserId { get; set; }

    /// <summary>Gets or sets the upload timestamp.</summary>
    public required DateTimeOffset UploadedAt { get; set; }
}

/// <summary>
/// Represents an audit event record in the backup manifest.
/// </summary>
public class BackupAuditEvent
{
    /// <summary>Gets or sets the event type.</summary>
    public required string EventType { get; set; }

    /// <summary>Gets or sets the acting user ID (if any).</summary>
    public Guid? ActingUserId { get; set; }

    /// <summary>Gets or sets the resource type (if any).</summary>
    public string? ResourceType { get; set; }

    /// <summary>Gets or sets the resource ID (if any).</summary>
    public Guid? ResourceId { get; set; }

    /// <summary>Gets or sets the outcome (Success or Failure).</summary>
    public required string Outcome { get; set; }

    /// <summary>Gets or sets the event timestamp.</summary>
    public required DateTimeOffset CreatedAt { get; set; }
}
