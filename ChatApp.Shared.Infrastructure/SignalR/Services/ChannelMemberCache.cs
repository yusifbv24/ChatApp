using Microsoft.Extensions.Caching.Memory;

namespace ChatApp.Shared.Infrastructure.SignalR.Services
{
    /// <summary>
    /// In-memory cache for channel member IDs
    /// Populated when:
    /// - User sends message to channel (SendChannelMessageCommand updates cache)
    /// - Members added/removed (AddMemberCommand/RemoveMemberCommand updates cache)
    /// - Channel selected (frontend can trigger cache refresh)
    ///
    /// PERFORMANCE: 5-minute expiration reduces stale data while maintaining typing indicator performance
    /// </summary>
    public class ChannelMemberCache : IChannelMemberCache
    {
        private readonly IMemoryCache _cache;
        // OPTIMIZED: Reduced from 30min to 5min to minimize stale member lists
        // Prevents typing notifications being sent to disconnected users
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public ChannelMemberCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<List<Guid>> GetChannelMemberIdsAsync(Guid channelId)
        {
            var cacheKey = GetCacheKey(channelId);

            if (_cache.TryGetValue(cacheKey, out List<Guid>? memberIds) && memberIds != null)
            {
                return Task.FromResult(memberIds);
            }

            // Return empty list if not cached
            // Will be populated on next message send or member change
            return Task.FromResult(new List<Guid>());
        }

        public Task UpdateChannelMembersAsync(Guid channelId, List<Guid> memberUserIds)
        {
            var cacheKey = GetCacheKey(channelId);

            // Configure cache with absolute expiration
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_cacheExpiration);

            _cache.Set(cacheKey, memberUserIds, cacheOptions);

            return Task.CompletedTask;
        }

        public Task RemoveChannelAsync(Guid channelId)
        {
            var cacheKey = GetCacheKey(channelId);
            _cache.Remove(cacheKey);
            return Task.CompletedTask;
        }

        private static string GetCacheKey(Guid channelId) => $"channel_members_{channelId}";
    }
}