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
                        join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                        from file in fileJoin.DefaultIfEmpty()
                        join repliedMessage in _context.DirectMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                        from repliedMessage in replyJoin.DefaultIfEmpty()
                        join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                        from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                        join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                        from repliedFile in repliedFileJoin.DefaultIfEmpty()
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
                            FileName = file != null ? file.OriginalFileName : null,
                            FileContentType = file != null ? file.ContentType : null,
                            FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                            message.IsEdited,
                            message.IsDeleted,
                            message.IsRead,
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.PinnedAtUtc,
                            ReactionCount = _context.DirectMessageReactions.Count(r => r.MessageId == message.Id),
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                            ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                            ReplyToSenderName = repliedSender != null ? repliedSender.DisplayName : null,
                            ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                            ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                            ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
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

            // Load mentions for this message
            var mentions = await _context.DirectMessageMentions
                .Where(m => m.MessageId == id)
                .Select(m => new MessageMentionDto(m.MentionedUserId, m.MentionedUserName))
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
                result.FileName,
                result.FileContentType,
                result.FileSizeInBytes,
                result.IsEdited,
                result.IsDeleted,
                result.IsRead,
                result.IsPinned,
                result.ReactionCount,
                result.CreatedAtUtc,
                result.EditedAtUtc,
                result.PinnedAtUtc,
                result.ReplyToMessageId,
                result.ReplyToIsDeleted ? "This message was deleted" : result.ReplyToContent, // SECURITY: Sanitize deleted reply content
                result.ReplyToSenderName,
                result.ReplyToFileId,
                result.ReplyToFileName,
                result.ReplyToFileContentType,
                result.IsForwarded,
                reactions.Count > 0 ? reactions : null,
                mentions.Count > 0 ? mentions : null
            );
        }


        public async Task<List<DirectMessageDto>> GetConversationMessagesAsync(
            Guid conversationId,
            int pageSize = 30,
            DateTime? beforeUtc = null,
            CancellationToken cancellationToken = default)
        {
            // Real database join with users table
            var query = from message in _context.DirectMessages
                        join sender in _context.Set<UserReadModel>() on message.SenderId equals sender.Id
                        join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                        from file in fileJoin.DefaultIfEmpty()
                        join repliedMessage in _context.DirectMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                        from repliedMessage in replyJoin.DefaultIfEmpty()
                        join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                        from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                        join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                        from repliedFile in repliedFileJoin.DefaultIfEmpty()
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
                            FileName = file != null ? file.OriginalFileName : null,
                            FileContentType = file != null ? file.ContentType : null,
                            FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                            message.IsEdited,
                            message.IsDeleted,
                            message.IsRead,
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.PinnedAtUtc,
                            ReactionCount = _context.DirectMessageReactions.Count(r => r.MessageId == message.Id),
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                            ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                            ReplyToSenderName = repliedSender != null ? repliedSender.DisplayName : null,
                            ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                            ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                            ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
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

            // Get message IDs for reactions and mentions lookup
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

            // Load mentions grouped by message
            var mentions = await _context.DirectMessageMentions
                .Where(m => messageIds.Contains(m.MessageId))
                .GroupBy(m => m.MessageId)
                .Select(g => new
                {
                    MessageId = g.Key,
                    Mentions = g.Select(m => new MessageMentionDto(m.MentionedUserId, m.MentionedUserName)).ToList()
                })
                .ToDictionaryAsync(x => x.MessageId, x => x.Mentions, cancellationToken);

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
                r.FileName,
                r.FileContentType,
                r.FileSizeInBytes,
                r.IsEdited,
                r.IsDeleted,
                r.IsRead,
                r.IsPinned,
                r.ReactionCount,
                r.CreatedAtUtc,
                r.EditedAtUtc,
                r.PinnedAtUtc,
                r.ReplyToMessageId,
                r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent, // SECURITY: Sanitize deleted reply content
                r.ReplyToSenderName,
                r.ReplyToFileId,
                r.ReplyToFileName,
                r.ReplyToFileContentType,
                r.IsForwarded,
                reactions.ContainsKey(r.Id) ? reactions[r.Id] : null,
                mentions.ContainsKey(r.Id) ? mentions[r.Id] : null
            )).ToList();
        }


        public async Task<List<DirectMessageDto>> GetMessagesAroundAsync(
            Guid conversationId,
            Guid messageId,
            int count = 50,
            CancellationToken cancellationToken = default)
        {
            // 1. Hədəf mesajın tarixini tap
            var targetMessage = await _context.DirectMessages
                .Where(m => m.Id == messageId && m.ConversationId == conversationId)
                .Select(m => new { m.CreatedAtUtc })
                .FirstOrDefaultAsync(cancellationToken);

            if (targetMessage == null)
                return new List<DirectMessageDto>();

            var targetDate = targetMessage.CreatedAtUtc;
            var halfCount = count / 2;

            // 2. Base query (projection)
            var baseQuery = from message in _context.DirectMessages
                           join sender in _context.Set<UserReadModel>() on message.SenderId equals sender.Id
                           join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                           from file in fileJoin.DefaultIfEmpty()
                           join repliedMessage in _context.DirectMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                           from repliedMessage in replyJoin.DefaultIfEmpty()
                           join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                           from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                           join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                           from repliedFile in repliedFileJoin.DefaultIfEmpty()
                           where message.ConversationId == conversationId
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
                               FileName = file != null ? file.OriginalFileName : null,
                               FileContentType = file != null ? file.ContentType : null,
                               FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                               message.IsEdited,
                               message.IsDeleted,
                               message.IsRead,
                               message.IsPinned,
                               message.CreatedAtUtc,
                               message.EditedAtUtc,
                               message.PinnedAtUtc,
                               ReactionCount = _context.DirectMessageReactions.Count(r => r.MessageId == message.Id),
                               message.ReplyToMessageId,
                               ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                               ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                               ReplyToSenderName = repliedSender != null ? repliedSender.DisplayName : null,
                               ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                               ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                               ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
                               message.IsForwarded
                           };

            // 3. Hədəf mesajdan ƏVVƏL olan mesajlar (hədəf daxil)
            var beforeMessages = await baseQuery
                .Where(m => m.CreatedAtUtc <= targetDate)
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(halfCount + 1)
                .ToListAsync(cancellationToken);

            // 4. Hədəf mesajdan SONRA olan mesajlar (hədəf istisna)
            var afterMessages = await baseQuery
                .Where(m => m.CreatedAtUtc > targetDate)
                .OrderBy(m => m.CreatedAtUtc)
                .Take(halfCount)
                .ToListAsync(cancellationToken);

            // 5. Birləşdir və sırala
            var results = beforeMessages.Concat(afterMessages)
                .OrderBy(m => m.CreatedAtUtc)
                .ToList();

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
                r.IsDeleted ? "This message was deleted" : r.Content,
                r.FileId,
                r.FileName,
                r.FileContentType,
                r.FileSizeInBytes,
                r.IsEdited,
                r.IsDeleted,
                r.IsRead,
                r.IsPinned,
                r.ReactionCount,
                r.CreatedAtUtc,
                r.EditedAtUtc,
                r.PinnedAtUtc,
                r.ReplyToMessageId,
                r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent,
                r.ReplyToSenderName,
                r.ReplyToFileId,
                r.ReplyToFileName,
                r.ReplyToFileContentType,
                r.IsForwarded,
                reactions.ContainsKey(r.Id) ? reactions[r.Id] : null
            )).ToList();
        }


        public async Task<List<DirectMessageDto>> GetMessagesBeforeDateAsync(
            Guid conversationId,
            DateTime beforeUtc,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            // Base query with all joins
            var query = from message in _context.DirectMessages
                        join sender in _context.Set<UserReadModel>() on message.SenderId equals sender.Id
                        join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                        from file in fileJoin.DefaultIfEmpty()
                        join repliedMessage in _context.DirectMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                        from repliedMessage in replyJoin.DefaultIfEmpty()
                        join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                        from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                        join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                        from repliedFile in repliedFileJoin.DefaultIfEmpty()
                        where message.ConversationId == conversationId
                           && message.CreatedAtUtc < beforeUtc
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
                            FileName = file != null ? file.OriginalFileName : null,
                            FileContentType = file != null ? file.ContentType : null,
                            FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                            message.IsEdited,
                            message.IsDeleted,
                            message.IsRead,
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.PinnedAtUtc,
                            ReactionCount = _context.DirectMessageReactions.Count(r => r.MessageId == message.Id),
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                            ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                            ReplyToSenderName = repliedSender != null ? repliedSender.DisplayName : null,
                            ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                            ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                            ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
                            message.IsForwarded
                        };

            var results = await query
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(limit)
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
                r.IsDeleted ? "This message was deleted" : r.Content,
                r.FileId,
                r.FileName,
                r.FileContentType,
                r.FileSizeInBytes,
                r.IsEdited,
                r.IsDeleted,
                r.IsRead,
                r.IsPinned,
                r.ReactionCount,
                r.CreatedAtUtc,
                r.EditedAtUtc,
                r.PinnedAtUtc,
                r.ReplyToMessageId,
                r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent,
                r.ReplyToSenderName,
                r.ReplyToFileId,
                r.ReplyToFileName,
                r.ReplyToFileContentType,
                r.IsForwarded,
                reactions.ContainsKey(r.Id) ? reactions[r.Id] : null
            )).ToList();
        }


        public async Task<List<DirectMessageDto>> GetMessagesAfterDateAsync(
            Guid conversationId,
            DateTime afterUtc,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            // Base query with all joins
            var query = from message in _context.DirectMessages
                        join sender in _context.Set<UserReadModel>() on message.SenderId equals sender.Id
                        join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                        from file in fileJoin.DefaultIfEmpty()
                        join repliedMessage in _context.DirectMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                        from repliedMessage in replyJoin.DefaultIfEmpty()
                        join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                        from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                        join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                        from repliedFile in repliedFileJoin.DefaultIfEmpty()
                        where message.ConversationId == conversationId
                           && message.CreatedAtUtc > afterUtc
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
                            FileName = file != null ? file.OriginalFileName : null,
                            FileContentType = file != null ? file.ContentType : null,
                            FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                            message.IsEdited,
                            message.IsDeleted,
                            message.IsRead,
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.PinnedAtUtc,
                            ReactionCount = _context.DirectMessageReactions.Count(r => r.MessageId == message.Id),
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                            ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                            ReplyToSenderName = repliedSender != null ? repliedSender.DisplayName : null,
                            ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                            ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                            ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
                            message.IsForwarded
                        };

            var results = await query
                .OrderBy(m => m.CreatedAtUtc)
                .Take(limit)
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
                r.IsDeleted ? "This message was deleted" : r.Content,
                r.FileId,
                r.FileName,
                r.FileContentType,
                r.FileSizeInBytes,
                r.IsEdited,
                r.IsDeleted,
                r.IsRead,
                r.IsPinned,
                r.ReactionCount,
                r.CreatedAtUtc,
                r.EditedAtUtc,
                r.PinnedAtUtc,
                r.ReplyToMessageId,
                r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent,
                r.ReplyToSenderName,
                r.ReplyToFileId,
                r.ReplyToFileName,
                r.ReplyToFileContentType,
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
                          join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                          from file in fileJoin.DefaultIfEmpty()
                          join repliedMessage in _context.DirectMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                          from repliedMessage in replyJoin.DefaultIfEmpty()
                          join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                          from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                          join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                          from repliedFile in repliedFileJoin.DefaultIfEmpty()
                          where message.ConversationId == conversationId
                             && message.IsPinned
                             && !message.IsDeleted
                          orderby message.PinnedAtUtc ascending
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
                              FileName = file != null ? file.OriginalFileName : null,
                              FileContentType = file != null ? file.ContentType : null,
                              FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                              message.IsEdited,
                              message.IsDeleted,
                              message.IsRead,
                              message.IsPinned,
                              message.CreatedAtUtc,
                              message.EditedAtUtc,
                              message.PinnedAtUtc,
                              ReactionCount = _context.DirectMessageReactions.Count(r => r.MessageId == message.Id),
                              message.ReplyToMessageId,
                              ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                              ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                              ReplyToSenderName = repliedSender != null ? repliedSender.DisplayName : null,
                              ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                              ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                              ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
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
                r.FileName,
                r.FileContentType,
                r.FileSizeInBytes,
                r.IsEdited,
                r.IsDeleted,
                r.IsRead,
                r.IsPinned,
                r.ReactionCount,
                r.CreatedAtUtc,
                r.EditedAtUtc,
                r.PinnedAtUtc,
                r.ReplyToMessageId,
                r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent, // SECURITY: Sanitize deleted reply content
                r.ReplyToSenderName,
                r.ReplyToFileId,
                r.ReplyToFileName,
                r.ReplyToFileContentType,
                r.IsForwarded,
                reactions.ContainsKey(r.Id) ? reactions[r.Id] : null
            )).ToList();
        }
    }
}