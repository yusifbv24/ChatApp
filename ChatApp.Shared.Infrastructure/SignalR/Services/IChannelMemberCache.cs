namespace ChatApp.Shared.Infrastructure.SignalR.Services
{
    /// <summary>
    /// Cache service for channel member IDs
    /// Used by ChatHub to get member lists for hybrid typing notifications
    /// without depending on Channels module repositories
    /// </summary>
    public interface IChannelMemberCache
    {
        /// <summary>
        /// Get list of active member user IDs for a channel
        /// </summary>
        Task<List<Guid>> GetChannelMemberIdsAsync(Guid channelId);

        /// <summary>
        /// Update cached member list for a channel
        /// Called when members are added/removed
        /// </summary>
        Task UpdateChannelMembersAsync(Guid channelId, List<Guid> memberUserIds);

        /// <summary>
        /// Remove channel from cache
        /// </summary>
        Task RemoveChannelAsync(Guid channelId);
    }
}
