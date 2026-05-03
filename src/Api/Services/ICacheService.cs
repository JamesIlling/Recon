namespace LocationManagement.Api.Services;

/// <summary>
/// Provides typed caching with support for both in-memory and distributed cache backends.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a cached value by key.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cached value, or null if not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class;

    /// <summary>
    /// Sets a value in the cache with a specified time-to-live.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="ttl">The time-to-live duration.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class;

    /// <summary>
    /// Invalidates a specific cache entry by key.
    /// </summary>
    /// <param name="key">The cache key to invalidate.</param>
    /// <param name="ct">Cancellation token.</param>
    Task InvalidateAsync(string key, CancellationToken ct);

    /// <summary>
    /// Invalidates all cache entries matching a key prefix.
    /// </summary>
    /// <param name="prefix">The key prefix to match.</param>
    /// <param name="ct">Cancellation token.</param>
    Task InvalidateByPrefixAsync(string prefix, CancellationToken ct);
}
