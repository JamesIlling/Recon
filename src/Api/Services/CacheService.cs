using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace LocationManagement.Api.Services;

/// <summary>
/// Implements caching with support for both in-memory and distributed backends.
/// </summary>
public sealed class CacheService : ICacheService
{
    private readonly IMemoryCache? _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly bool _useDistributed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService"/> class.
    /// </summary>
    public CacheService(IMemoryCache? memoryCache = null, IDistributedCache? distributedCache = null)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _useDistributed = distributedCache != null;
    }

    /// <summary>
    /// Retrieves a cached value by key.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
    {
        if (_useDistributed && _distributedCache != null)
        {
            var json = await _distributedCache.GetStringAsync(key, ct);
            if (json == null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json);
        }

        if (_memoryCache != null && _memoryCache.TryGetValue(key, out T? value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// Sets a value in the cache with a specified time-to-live.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class
    {
        if (_useDistributed && _distributedCache != null)
        {
            var json = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(key, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        else if (_memoryCache != null)
        {
            _memoryCache.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
        }
    }

    /// <summary>
    /// Invalidates a specific cache entry by key.
    /// </summary>
    public async Task InvalidateAsync(string key, CancellationToken ct)
    {
        if (_useDistributed && _distributedCache != null)
        {
            await _distributedCache.RemoveAsync(key, ct);
        }
        else if (_memoryCache != null)
        {
            _memoryCache.Remove(key);
        }
    }

    /// <summary>
    /// Invalidates all cache entries matching a key prefix.
    /// Note: Distributed cache does not support prefix invalidation natively.
    /// This is a limitation of the distributed cache backend.
    /// </summary>
    public async Task InvalidateByPrefixAsync(string prefix, CancellationToken ct)
    {
        if (_useDistributed && _distributedCache != null)
        {
            // Distributed cache does not support prefix-based invalidation
            // This would require a custom implementation or a different cache backend
            // For now, this is a no-op
            await Task.CompletedTask;
        }
        else if (_memoryCache != null)
        {
            // IMemoryCache does not expose a way to enumerate keys
            // This is a limitation of the in-memory cache
            // For now, this is a no-op
            await Task.CompletedTask;
        }
    }
}
