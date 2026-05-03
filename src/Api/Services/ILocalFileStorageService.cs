namespace LocationManagement.Api.Services;

/// <summary>
/// Stores and retrieves files from local filesystem storage.
/// </summary>
public interface ILocalFileStorageService
{
    /// <summary>
    /// Stores a file and returns the relative storage path.
    /// </summary>
    /// <param name="fileId">A unique identifier for the file (typically a GUID).</param>
    /// <param name="fileStream">The file content stream.</param>
    /// <param name="fileName">The original file name (used for extension).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The relative storage path for the file.</returns>
    Task<string> StoreAsync(Guid fileId, Stream fileStream, string fileName, CancellationToken ct);

    /// <summary>
    /// Retrieves a file stream by its storage path.
    /// </summary>
    /// <param name="storagePath">The relative storage path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The file stream, or null if not found.</returns>
    Task<Stream?> RetrieveAsync(string storagePath, CancellationToken ct);

    /// <summary>
    /// Deletes a file by its storage path.
    /// </summary>
    /// <param name="storagePath">The relative storage path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string storagePath, CancellationToken ct);
}
