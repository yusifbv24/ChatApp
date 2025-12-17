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
    }
}