 namespace ChatApp.Shared.Infrastructure.Caching;

/// <summary>
/// Builds module-scoped cache keys to prevent key collisions across modules.
///
/// Key pattern: {module}:{part1}:{part2}:...
///
/// Examples:
///   CacheKeyBuilder.Build("identity", "user", userId)        → "identity:user:{userId}"
///   CacheKeyBuilder.Build("channels", "members", channelId)  → "channels:members:{channelId}"
///   CacheKeyBuilder.Build("dm", "conversation", convId)      → "dm:conversation:{convId}"
///   CacheKeyBuilder.Build("notifications", "unread", userId) → "notifications:unread:{userId}"
/// </summary>
public static class CacheKeyBuilder
{
    /// <summary>
    /// Builds a cache key with module prefix and variable parts.
    /// </summary>
    /// <param name="module">Module name (e.g., "identity", "channels", "dm")</param>
    /// <param name="parts">Key segments joined by colon</param>
    public static string Build(string module, params string[] parts)
        => $"{module}:{string.Join(":", parts)}";
}