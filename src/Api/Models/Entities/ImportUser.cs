namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents a system-managed user account created during an import operation.
/// ImportUsers track import sessions and serve as the owner for imported resources
/// when the original owner cannot be matched to an existing user in the system.
/// </summary>
public class ImportUser
{
    /// <summary>
    /// Gets or sets the unique identifier for the import user.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the name of the import session (e.g., "Import from backup 2025-01-15").
    /// </summary>
    public required string ImportSessionName { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the import was performed.
    /// </summary>
    public required DateTimeOffset ImportedAt { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the admin user who initiated the import.
    /// Foreign key to User.Id.
    /// </summary>
    public required Guid ImportedByUserId { get; init; }

    /// <summary>
    /// Gets or sets the navigation property for the admin user who initiated the import.
    /// </summary>
    public virtual User ImportedByUser { get; set; } = null!;
}
