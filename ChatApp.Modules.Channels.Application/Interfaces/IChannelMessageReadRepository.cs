using ChatApp.Modules.Channels.Domain.Entities;

namespace ChatApp.Modules.Channels.Application.Interfaces
{
    public interface IChannelMessageReadRepository
    {
        /// <summary>
        /// Checks if a specific message has been read by a user
        /// </summary>
        Task<bool> ExistsAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk inserts multiple ChannelMessageRead records
        /// </summary>
        Task BulkInsertAsync(List<ChannelMessageRead> reads, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the IDs of unread messages in a channel for a specific user
        /// </summary>
        Task<List<Guid>> GetUnreadMessageIdsAsync(Guid channelId, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a single read record
        /// </summary>
        Task AddAsync(ChannelMessageRead read, CancellationToken cancellationToken = default);
    }
}
