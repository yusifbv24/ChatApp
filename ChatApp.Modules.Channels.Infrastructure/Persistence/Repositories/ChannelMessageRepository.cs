using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Repositories
{
    public class ChannelMessageRepository : IChannelMessageRepository
    {
        private readonly ChannelsDbContext _context;

        public ChannelMessageRepository(ChannelsDbContext context)
        {
            _context = context;
        }

        public async Task<ChannelMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ChannelMessages
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<ChannelMessage?> GetByIdWithReactionsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ChannelMessages
                .Include(m => m.Reactions)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<List<ChannelMessageDto>> GetChannelMessagesAsync(
            Guid channelId,
            int pageSize = 50,
            DateTime? beforeUtc = null,
            CancellationToken cancellationToken = default)
        {
            // Real database join with users table
            var query = from message in _context.ChannelMessages
                        join user in _context.Set<UserReadModel>() on message.SenderId equals user.Id
                        join repliedMessage in _context.ChannelMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                        from repliedMessage in replyJoin.DefaultIfEmpty()
                        join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                        from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                        where message.ChannelId == channelId // Removed IsDeleted filter - show deleted messages as "This message was deleted"
                        select new
                        {
                            message.Id,
                            message.ChannelId,
                            message.SenderId,
                            user.Username,
                            user.DisplayName,
                            user.AvatarUrl,
                            message.Content,
                            message.FileId,
                            message.IsEdited,
                            message.IsDeleted,
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.PinnedAtUtc,
                            ReactionCount = _context.ChannelMessageReactions.Count(r => r.MessageId == message.Id),
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null ? repliedMessage.Content : null,
                            ReplyToSenderName = repliedSender != null ? repliedSender.DisplayName : null,
                            message.IsForwarded
                        };

            if (beforeUtc.HasValue)
            {
                query = query.Where(m => m.CreatedAtUtc < beforeUtc.Value);
            }

            var results = await query
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return results.Select(r => new ChannelMessageDto(
                r.Id,
                r.ChannelId,
                r.SenderId,
                r.Username,
                r.DisplayName,
                r.AvatarUrl,
                r.Content,
                r.FileId,
                r.IsEdited,
                r.IsDeleted,
                r.IsPinned,
                r.ReactionCount,
                r.CreatedAtUtc,
                r.EditedAtUtc,
                r.PinnedAtUtc,
                r.ReplyToMessageId,
                r.ReplyToContent,
                r.ReplyToSenderName,
                r.IsForwarded
            )).ToList();
        }

        public async Task<List<ChannelMessageDto>> GetPinnedMessagesAsync(Guid channelId, CancellationToken cancellationToken = default)
        {
            // Database join to get pinned messages with user details
            return await (from message in _context.ChannelMessages
                          join user in _context.Set<UserReadModel>() on message.SenderId equals user.Id
                          join repliedMessage in _context.ChannelMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                          from repliedMessage in replyJoin.DefaultIfEmpty()
                          join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                          from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                          where message.ChannelId == channelId
                             && message.IsPinned
                             && !message.IsDeleted
                          orderby message.PinnedAtUtc descending
                          select new ChannelMessageDto(
                              message.Id,
                              message.ChannelId,
                              message.SenderId,
                              user.Username,
                              user.DisplayName,
                              user.AvatarUrl,
                              message.Content,
                              message.FileId,
                              message.IsEdited,
                              message.IsDeleted,
                              message.IsPinned,
                              _context.ChannelMessageReactions.Count(r => r.MessageId == message.Id),
                              message.CreatedAtUtc,
                              message.EditedAtUtc,
                              message.PinnedAtUtc,
                              message.ReplyToMessageId,
                              repliedMessage != null ? repliedMessage.Content : null,
                              repliedSender != null ? repliedSender.DisplayName : null,
                              message.IsForwarded
                          ))
                         .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(Guid channelId, Guid userId, CancellationToken cancellationToken = default)
        {
            // Get the last read message timestamp for this user in this channel
            var lastReadTime = await _context.ChannelMessageReads
                .Where(r => r.UserId == userId)
                .Join(_context.ChannelMessages,
                    read => read.MessageId,
                    message => message.Id,
                    (read, message) => new { read.ReadAtUtc, message.ChannelId })
                .Where(x => x.ChannelId == channelId)
                .OrderByDescending(x => x.ReadAtUtc)
                .Select(x => (DateTime?)x.ReadAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            // Count unread messages (after last read time, excluding user's own messages)
            var query = _context.ChannelMessages
                .Where(m => m.ChannelId == channelId && !m.IsDeleted && m.SenderId != userId);

            if (lastReadTime.HasValue)
            {
                query = query.Where(m => m.CreatedAtUtc > lastReadTime.Value);
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task AddAsync(ChannelMessage message, CancellationToken cancellationToken = default)
        {
            await _context.ChannelMessages.AddAsync(message, cancellationToken);
        }

        public Task UpdateAsync(ChannelMessage message, CancellationToken cancellationToken = default)
        {
            _context.ChannelMessages.Update(message);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ChannelMessage message, CancellationToken cancellationToken = default)
        {
            _context.ChannelMessages.Remove(message);
            return Task.CompletedTask;
        }
    }
}