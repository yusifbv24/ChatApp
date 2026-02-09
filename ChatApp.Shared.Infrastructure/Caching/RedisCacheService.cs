using ChatApp.Shared.Kernel.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ChatApp.Shared.Infrastructure.Caching;

/// <summary>
/// Redis-backed implementation of ICacheService.
/// Provides type-safe caching with JSON serialization.
/// Thread-safe — IDistributedCache handles concurrency internally.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await _cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrEmpty(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);

        var options = new DistributedCacheEntryOptions();

        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry.Value;
        }

        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetAsync(key, cancellationToken);
        return value != null;
    }
}