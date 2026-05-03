namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Represents the summary result of an import operation.
/// Contains counts of imported/skipped records and any warnings encountered.
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Gets or sets the ID of the ImportUser created for this import session.
    /// </summary>
    public required Guid ImportUserId { get; set; }

    /// <summary>
    /// Gets or sets the number of users imported.
    /// </summary>
    public required int UsersImported { get; set; }

    /// <summary>
    /// Gets or sets the number of users skipped (already exist).
    /// </summary>
    public required int UsersSkipped { get; set; }

    /// <summary>
    /// Gets or sets the number of locations imported.
    /// </summary>
    public required int LocationsImported { get; set; }

    /// <summary>
    /// Gets or sets the number of locations skipped (invalid).
    /// </summary>
    public required int LocationsSkipped { get; set; }

    /// <summary>
    /// Gets or sets the number of location collections imported.
    /// </summary>
    public required int CollectionsImported { get; set; }

    /// <summary>
    /// Gets or sets the number of location collections skipped (invalid).
    /// </summary>
    public required int CollectionsSkipped { get; set; }

    /// <summary>
    /// Gets or sets the number of collection members imported.
    /// </summary>
    public required int MembersImported { get; set; }

    /// <summary>
    /// Gets or sets the number of collection members skipped (invalid).
    /// </summary>
    public required int MembersSkipped { get; set; }

    /// <summary>
    /// Gets or sets the number of named shapes imported.
    /// </summary>
    public required int NamedShapesImported { get; set; }

    /// <summary>
    /// Gets or sets the number of named shapes skipped (invalid).
    /// </summary>
    public required int NamedShapesSkipped { get; set; }

    /// <summary>
    /// Gets or sets the number of images imported.
    /// </summary>
    public required int ImagesImported { get; set; }

    /// <summary>
    /// Gets or sets the number of images skipped (invalid).
    /// </summary>
    public required int ImagesSkipped { get; set; }

    /// <summary>
    /// Gets or sets the collection of warning messages encountered during import.
    /// </summary>
    public required List<string> Warnings { get; set; }
}
