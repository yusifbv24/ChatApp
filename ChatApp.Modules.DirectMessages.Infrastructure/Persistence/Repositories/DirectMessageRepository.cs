using ChatApp.Modules.DirectMessages.Application.DTOs.Request;
using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Repositories
{
    public class DirectMessageRepository:IDirectMessageRepository
    {
        private readonly DirectMessagesDbContext _context;

        public DirectMessageRepository(DirectMessagesDbContext context)
        {
            _context= context;
        }

        public async Task AddAsync(DirectMessage message, CancellationToken cancellationToken = default)
        {
            await _context.DirectMessages.AddAsync(message,cancellationToken);
        }


        public Task DeleteAsync(DirectMessage message, CancellationToken cancellationToken = default)
        {
            _context.DirectMessages.Remove(message);
            return Task.CompletedTask;
        }


        public async Task<DirectMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.DirectMessages
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }


        public async Task<DirectMessage?> GetByIdWithReactionsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.DirectMessages
                .Include(m=>m.Reactions)
                .FirstOrDefaultAsync(m=>m.Id==id,cancellationToken);
        }


        public async Task<List<DirectMessageDto>> GetConversationMessagesAsync(
            Guid conversationId, 
            int pageSize = 50, 
            DateTime? beforeUtc = null, 
            CancellationToken cancellationToken = default)
        {
            // Real database join with users table
            var query = from message in _context.DirectMessages
                        join sender in _context.Set<UserReadModel>() on message.SenderId equals sender.Id
                        join repliedMessage in _context.DirectMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                        from repliedMessage in replyJoin.DefaultIfEmpty()
                        join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                        from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                        where message.ConversationId == conversationId && !message.IsDeleted
                        select new
                        {
                            message.Id,
                            message.ConversationId,
                            message.SenderId,
                            sender.Username,
                            sender.DisplayName,
                            sender.AvatarUrl,
                            message.ReceiverId,
                            message.Content,
                            message.FileId,
                            message.IsEdited,
                            message.IsDeleted,
                            message.IsRead,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.ReadAtUtc,
                            ReactionCount = _context.DirectMessageReactions.Count(r => r.MessageId == message.Id),
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

            return results.Select(r => new DirectMessageDto(
                r.Id,
                r.ConversationId,
                r.SenderId,
                r.Username,
                r.DisplayName,
                r.AvatarUrl,
                r.ReceiverId,
                r.Content,
                r.FileId,
                r.IsEdited,
                r.IsDeleted,
                r.IsRead,
                r.ReactionCount,
                r.CreatedAtUtc,
                r.EditedAtUtc,
                r.ReadAtUtc,
                r.ReplyToMessageId,
                r.ReplyToContent,
                r.ReplyToSenderName,
                r.IsForwarded
            )).ToList();
        }


        public async Task<int> GetUnreadCountAsync(
            Guid conversationId, 
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            return await _context.DirectMessages
                .Where(m => m.ConversationId == conversationId &&
                       m.ReceiverId == userId &&
                       !m.IsRead &&
                       !m.IsDeleted)
                .CountAsync(cancellationToken);
        }


        public Task UpdateAsync(DirectMessage message, CancellationToken cancellationToken = default)
        {
            _context.DirectMessages.Update(message);
            return Task.CompletedTask;
        }
    }
}