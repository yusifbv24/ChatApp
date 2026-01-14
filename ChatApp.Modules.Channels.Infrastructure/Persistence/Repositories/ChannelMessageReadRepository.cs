using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Repositories
{
    public class ChannelMessageReadRepository : IChannelMessageReadRepository
    {
        private readonly ChannelsDbContext _context;

        public ChannelMessageReadRepository(ChannelsDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.ChannelMessageReads
                .AnyAsync(r => r.MessageId == messageId && r.UserId == userId, cancellationToken);
        }

        public async Task BulkInsertAsync(List<ChannelMessageRead> reads, CancellationToken cancellationToken = default)
        {
            if (reads == null || reads.Count == 0)
                return;

            await _context.ChannelMessageReads.AddRangeAsync(reads, cancellationToken);
        }

        public async Task<List<Guid>> GetUnreadMessageIdsAsync(Guid channelId, Guid userId, CancellationToken cancellationToken = default)
        {
            // Get all message IDs in the channel that haven't been read by the user
            var unreadMessageIds = await _context.ChannelMessages
                .Where(m => m.ChannelId == channelId && !m.IsDeleted)
                .Where(m => !_context.ChannelMessageReads
                    .Any(r => r.MessageId == m.Id && r.UserId == userId))
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            return unreadMessageIds;
        }

        public async Task AddAsync(ChannelMessageRead read, CancellationToken cancellationToken = default)
        {
            await _context.ChannelMessageReads.AddAsync(read, cancellationToken);
        }

        public async Task<int> GetReadByCountAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            return await _context.ChannelMessageReads
                .CountAsync(r => r.MessageId == messageId, cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetReadByCountsAsync(List<Guid> messageIds, CancellationToken cancellationToken = default)
        {
            if (messageIds == null || messageIds.Count == 0)
                return new Dictionary<Guid, int>();

            // Get read counts from database (only for messages that have been read)
            var readCounts = await _context.ChannelMessageReads
                .Where(r => messageIds.Contains(r.MessageId))
                .GroupBy(r => r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            // Add missing messages with 0 count (messages that haven't been read yet)
            var result = new Dictionary<Guid, int>();
            foreach (var messageId in messageIds)
            {
                result[messageId] = readCounts.TryGetValue(messageId, out var count) ? count : 0;
            }

            return result;
        }
    }
}