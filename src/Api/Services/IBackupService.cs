using LocationManagement.Api.Models.Dtos;

namespace LocationManagement.Api.Services;

/// <summary>
/// Defines operations for exporting and importing encrypted backup archives.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Exports all exportable data (Users, Locations, LocationCollections, CollectionMembers, NamedShapes, Images, AuditEvents)
    /// into a JSON manifest, packages it with image files into a ZIP archive, encrypts with AES-256, and returns the encrypted stream.
    /// </summary>
    /// <param name="encryptionKey">The encryption key (minimum 32 characters). Never logged.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An encrypted stream containing the backup archive.</returns>
    /// <exception cref="ArgumentException">Thrown if encryptionKey is less than 32 characters.</exception>
    Task<Stream> ExportAsync(string encryptionKey, CancellationToken ct);

    /// <summary>
    /// Imports data from an encrypted backup archive.
    /// Decrypts the stream, validates the JSON schema, creates an ImportUser record, and imports data additively with new IDs.
    /// Skips existing users, creates new Locations/Collections with new IDs, validates coordinates and images.
    /// </summary>
    /// <param name="encryptionKey">The decryption key (minimum 32 characters). Never logged.</param>
    /// <param name="importStream">The encrypted backup stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An ImportResult summary with counts and warnings.</returns>
    /// <exception cref="ArgumentException">Thrown if encryptionKey is less than 32 characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown if schema validation fails (HTTP 422 on controller).</exception>
    Task<ImportResult> ImportAsync(string encryptionKey, Stream importStream, CancellationToken ct);
}
