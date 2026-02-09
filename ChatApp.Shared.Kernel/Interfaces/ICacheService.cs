namespace ChatApp.Shared.Kernel.Interfaces;

/// <summary>
/// Generic distributed cache abstraction for all modules.
/// Backed by Redis — modules don't need to know about the underlying implementation.
/// Use CacheKeyBuilder to generate module-scoped keys and avoid collisions.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key. Returns null if not found or expired.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in cache with optional expiration.
    /// If expiry is null, the value never expires (until eviction).
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}