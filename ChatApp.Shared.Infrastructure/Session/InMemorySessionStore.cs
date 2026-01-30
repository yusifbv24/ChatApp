using ChatApp.Shared.Kernel.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ChatApp.Shared.Infrastructure.Session;

public class InMemorySessionStore : ISessionStore
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _userSessions = new();
    private readonly object _lock = new();

    public InMemorySessionStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string CreateSession(Guid userId, string accessToken, string refreshToken, TimeSpan accessTokenLifetime, TimeSpan refreshTokenLifetime)
    {
        var sessionId = GenerateOpaqueId();

        var sessionData = new SessionData
        {
            UserId = userId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.Add(accessTokenLifetime),
            RefreshTokenExpiresAt = DateTime.UtcNow.Add(refreshTokenLifetime)
        };

        // Session lives as long as the refresh token
        _cache.Set(CacheKey(sessionId), sessionData, refreshTokenLifetime);

        // Track user → sessions mapping
        _userSessions.AddOrUpdate(userId,
            _ => new HashSet<string> { sessionId },
            (_, set) => { lock (_lock) { set.Add(sessionId); } return set; });

        return sessionId;
    }

    public string? GetAccessToken(string sessionId)
    {
        if (_cache.TryGetValue<SessionData>(CacheKey(sessionId), out var data))
        {
            if (data!.AccessTokenExpiresAt > DateTime.UtcNow)
                return data.AccessToken;
        }
        return null;
    }

    public string? GetRefreshToken(string sessionId)
    {
        if (_cache.TryGetValue<SessionData>(CacheKey(sessionId), out var data))
        {
            if (data!.RefreshTokenExpiresAt > DateTime.UtcNow)
                return data.RefreshToken;
        }
        return null;
    }

    public void UpdateTokens(string sessionId, string newAccessToken, string newRefreshToken, TimeSpan accessTokenLifetime, TimeSpan refreshTokenLifetime)
    {
        if (_cache.TryGetValue<SessionData>(CacheKey(sessionId), out var data))
        {
            data!.AccessToken = newAccessToken;
            data.RefreshToken = newRefreshToken;
            data.AccessTokenExpiresAt = DateTime.UtcNow.Add(accessTokenLifetime);
            data.RefreshTokenExpiresAt = DateTime.UtcNow.Add(refreshTokenLifetime);

            _cache.Set(CacheKey(sessionId), data, refreshTokenLifetime);
        }
    }

    public void RemoveSession(string sessionId)
    {
        if (_cache.TryGetValue<SessionData>(CacheKey(sessionId), out var data))
        {
            _cache.Remove(CacheKey(sessionId));

            if (_userSessions.TryGetValue(data!.UserId, out var sessions))
            {
                lock (_lock) { sessions.Remove(sessionId); }
            }
        }
    }

    public void RemoveAllUserSessions(Guid userId)
    {
        if (_userSessions.TryRemove(userId, out var sessions))
        {
            lock (_lock)
            {
                foreach (var sid in sessions)
                {
                    _cache.Remove(CacheKey(sid));
                }
            }
        }
    }

    private static string GenerateOpaqueId()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string CacheKey(string sessionId) => $"session:{sessionId}";

    private class SessionData
    {
        public Guid UserId { get; set; }
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}