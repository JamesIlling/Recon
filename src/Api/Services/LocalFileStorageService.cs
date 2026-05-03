using Microsoft.Extensions.Logging;

namespace LocationManagement.Api.Services;

/// <summary>
/// Stores and retrieves files from local filesystem storage.
/// </summary>
public sealed class LocalFileStorageService : ILocalFileStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _storagePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileStorageService"/> class.
    /// </summary>
    public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _storagePath = Environment.GetEnvironmentVariable("IMAGES_STORAGE_PATH") ?? Path.Combine(Path.GetTempPath(), "LocationManagement", "Images");

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    /// <summary>
    /// Stores a file and returns the relative storage path.
    /// </summary>
    public async Task<string> StoreAsync(Guid fileId, Stream fileStream, string fileName, CancellationToken ct)
    {
        var extension = Path.GetExtension(fileName);
        var fileDirectory = Path.Combine(_storagePath, fileId.ToString());

        if (!Directory.Exists(fileDirectory))
        {
            Directory.CreateDirectory(fileDirectory);
        }

        var filePath = Path.Combine(fileDirectory, $"original{extension}");

        using (var fileWriter = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(fileWriter, ct);
        }

        _logger.LogInformation("File stored at {FilePath}", filePath);
        return Path.Combine(fileId.ToString(), $"original{extension}");
    }

    /// <summary>
    /// Retrieves a file stream by its storage path.
    /// </summary>
    public async Task<Stream?> RetrieveAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_storagePath, storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found at {FilePath}", fullPath);
            return null;
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return await Task.FromResult(stream);
    }

    /// <summary>
    /// Deletes a file by its storage path.
    /// </summary>
    public async Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_storagePath, storagePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted at {FilePath}", fullPath);
        }

        await Task.CompletedTask;
    }
}
