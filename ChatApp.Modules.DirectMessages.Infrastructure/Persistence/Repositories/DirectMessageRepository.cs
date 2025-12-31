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


        public Task UpdateAsync(DirectMessage message, CancellationToken cancellationToken = default)
        {
            _context.DirectMessages.Update(message);
            return Task.CompletedTask;
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


        public async Task<DirectMessageDto?> GetByIdAsDtoAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await (from message in _context.DirectMessages
                        join sender in _context.Set<UserReadModel>() on message.SenderId equals sender.Id
                        join repliedMessage in _context.DirectMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                        from repliedMessage in replyJoin.DefaultIfEmpty()
                        join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                        from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                        where message.Id == id
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
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.ReadAtUtc,
                            message.PinnedAtUtc,
                            ReactionCount = _context.DirectMessageReactions.Count(r => r.MessageId == message.Id),
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                            ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                            ReplyToSenderName = repliedSender != null ? repliedSender.DisplayName : null,
                            message.IsForwarded
                        }).FirstOrDefaultAsync(cancellationToken);

            if (result == null)
                return null;

            // Load reactions for this message
            var reactions = await _context.DirectMessageReactions
                .Where(r => r.MessageId == id)
                .GroupBy(r => r.Reaction)
                .Select(rg => new DirectMessageReactionDto(
                    rg.Key,
                    rg.Count(),
                    rg.Select(r => r.UserId).ToList()
                ))
                .ToListAsync(cancellationToken);

            return new DirectMessageDto(
                result.Id,
                result.ConversationId,
                result.SenderId,
                result.Username,
                result.DisplayName,
                result.AvatarUrl,
                result.ReceiverId,
                result.IsDeleted ? "This message was deleted" : result.Content, // SECURITY: Sanitize deleted content
                result.FileId,
                result.IsEdited,
                result.IsDeleted,
                result.IsRead,
                result.IsPinned,
                result.ReactionCount,
                result.CreatedAtUtc,
                result.EditedAtUtc,
                result.ReadAtUtc,
                result.PinnedAtUtc,
                result.ReplyToMessageId,
                result.ReplyToIsDeleted ? "This message was deleted" : result.ReplyToContent, // SECURITY: Sanitize deleted reply content
                result.ReplyToSenderName,
                result.IsForwarded,
                reactions.Count > 0 ? reactions : null
            );
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
                        where message.ConversationId == conversationId // Removed IsDeleted filter - show deleted messages as "This message was deleted"
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
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.ReadAtUtc,
                            message.PinnedAtUtc,
                            ReactionCount = _context.DirectMessageReactions.Count(r => r.MessageId == message.Id),
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                            ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
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

            // Get message IDs for reactions lookup
            var messageIds = results.Select(r => r.Id).ToList();

            // Load reactions grouped by message
            var reactions = await _context.DirectMessageReactions
                .Where(r => messageIds.Contains(r.MessageId))
                .GroupBy(r => r.MessageId)
                .Select(g => new
                {
                    MessageId = g.Key,
                    Reactions = g.GroupBy(r => r.Reaction)
                        .Select(rg => new DirectMessageReactionDto(
                            rg.Key,
                            rg.Count(),
                            rg.Select(r => r.UserId).ToList()
                        )).ToList()
                })
                .ToDictionaryAsync(x => x.MessageId, x => x.Reactions, cancellationToken);

            return results.Select(r => new DirectMessageDto(
                r.Id,
                r.ConversationId,
                r.SenderId,
                r.Username,
                r.DisplayName,
                r.AvatarUrl,
                r.ReceiverId,
                r.IsDeleted ? "This message was deleted" : r.Content, // SECURITY: Sanitize deleted content
                r.FileId,
                r.IsEdited,
                r.IsDeleted,
                r.IsRead,
                r.IsPinned,
                r.ReactionCount,
                r.CreatedAtUtc,
                r.EditedAtUtc,
                r.ReadAtUtc,
                r.PinnedAtUtc,
                r.ReplyToMessageId,
                r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent, // SECURITY: Sanitize deleted reply content
                r.ReplyToSenderName,
                r.IsForwarded,
                reactions.ContainsKey(r.Id) ? reactions[r.Id] : null
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


        public async Task<List<DirectMessage>> GetUnreadMessagesForUserAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.DirectMessages
                .Where(m => m.ConversationId == conversationId &&
                       m.ReceiverId == userId &&
                       !m.IsRead &&
                       !m.IsDeleted)
                .OrderBy(m => m.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }


        public async Task<List<DirectMessageDto>> GetPinnedMessagesAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
        {
            // Database join to get pinned messages with user details
            var results = await (from message in _context.DirectMessages
                          join sender in _context.Set<UserReadModel>() on message.SenderId equals sender.Id
                          join repliedMessage in _context.DirectMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                          from repliedMessage in replyJoin.DefaultIfEmpty()
                          join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                          from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                          where message.ConversationId == conversationId
                             && message.IsPinned
                             && !message.IsDeleted
                          orderby message.PinnedAtUtc descending
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
                              message.IsPinned,
                              message.CreatedAtUtc,
                              message.EditedAtUtc,
                              message.ReadAtUtc,
                              message.PinnedAtUtc,
                              ReactionCount = _context.DirectMessageReactions.Count(r => r.MessageId == message.Id),
                              message.ReplyToMessageId,
                              ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                              ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                              ReplyToSenderName = repliedSender != null ? repliedSender.DisplayName : null,
                              message.IsForwarded
                          }).ToListAsync(cancellationToken);

            // Get message IDs for reactions lookup
            var messageIds = results.Select(r => r.Id).ToList();

            // Load reactions grouped by message
            var reactions = await _context.DirectMessageReactions
                .Where(r => messageIds.Contains(r.MessageId))
                .GroupBy(r => r.MessageId)
                .Select(g => new
                {
                    MessageId = g.Key,
                    Reactions = g.GroupBy(r => r.Reaction)
                        .Select(rg => new DirectMessageReactionDto(
                            rg.Key,
                            rg.Count(),
                            rg.Select(r => r.UserId).ToList()
                        )).ToList()
                })
                .ToDictionaryAsync(x => x.MessageId, x => x.Reactions, cancellationToken);

            return results.Select(r => new DirectMessageDto(
                r.Id,
                r.ConversationId,
                r.SenderId,
                r.Username,
                r.DisplayName,
                r.AvatarUrl,
                r.ReceiverId,
                r.Content, // Pinned messages are not deleted, no sanitization needed
                r.FileId,
                r.IsEdited,
                r.IsDeleted,
                r.IsRead,
                r.IsPinned,
                r.ReactionCount,
                r.CreatedAtUtc,
                r.EditedAtUtc,
                r.ReadAtUtc,
                r.PinnedAtUtc,
                r.ReplyToMessageId,
                r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent, // SECURITY: Sanitize deleted reply content
                r.ReplyToSenderName,
                r.IsForwarded,
                reactions.ContainsKey(r.Id) ? reactions[r.Id] : null
            )).ToList();
        }
    }
}