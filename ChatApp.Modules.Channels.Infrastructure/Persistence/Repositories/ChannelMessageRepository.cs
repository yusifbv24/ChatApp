using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ChatApp.Shared.Kernel;

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
                .AsNoTracking()
                .Include(m => m.Reactions)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<ChannelMessageDto?> GetByIdAsDtoAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await (from message in _context.ChannelMessages
                        join user in _context.Set<UserReadModel>() on message.SenderId equals user.Id
                        join repliedMessage in _context.ChannelMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                        from repliedMessage in replyJoin.DefaultIfEmpty()
                        join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                        from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                        join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                        from file in fileJoin.DefaultIfEmpty()
                        join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                        from repliedFile in repliedFileJoin.DefaultIfEmpty()
                        where message.Id == id
                        select new
                        {
                            message.Id,
                            message.ChannelId,
                            message.SenderId,
                            user.Email,
                            user.FullName,
                            user.AvatarUrl,
                            message.Content,
                            message.FileId,
                            FileName = file != null ? file.OriginalFileName : null,
                            FileContentType = file != null ? file.ContentType : null,
                            FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                            FileStoragePath = file != null ? file.StoragePath : null,
                            FileThumbnailPath = file != null ? file.ThumbnailPath : null,
                            message.IsEdited,
                            message.IsDeleted,
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.PinnedAtUtc,
                            // REMOVED: ReactionCount N+1 query - now batched
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                            ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                            ReplyToSenderName = repliedSender != null ? repliedSender.FullName : null,
                            ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                            ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                            ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
                            ReplyToFileStoragePath = repliedFile != null ? repliedFile.StoragePath : null,
                            ReplyToFileThumbnailPath = repliedFile != null ? repliedFile.ThumbnailPath : null,
                            message.IsForwarded,
                            ReadByCount = _context.ChannelMessageReads.Count(r =>
                                r.MessageId == message.Id &&
                                r.UserId != message.SenderId),
                            TotalMemberCount = _context.ChannelMembers.Count(m =>
                                m.ChannelId == message.ChannelId &&
                                m.IsActive &&
                                m.UserId != message.SenderId),
                            ReadBy = _context.ChannelMessageReads
                                .Where(r => r.MessageId == message.Id && r.UserId != message.SenderId)
                                .Select(r => r.UserId)
                                .ToList()
                        }).FirstOrDefaultAsync(cancellationToken);

            if (result == null)
                return null;

            // Load mentions for this message
            var mentions = await _context.ChannelMessageMentions
                .Where(m => m.MessageId == id)
                .Select(m => new ChannelMessageMentionDto(m.MentionedUserId, m.MentionedUserFullName, m.IsAllMention))
                .ToListAsync(cancellationToken);

            // Load reactions for this message
            var reactions = await (from reaction in _context.ChannelMessageReactions
                                  join reactionUser in _context.Set<UserReadModel>() on reaction.UserId equals reactionUser.Id
                                  where reaction.MessageId == id
                                  group new { reaction, reactionUser } by reaction.Reaction into g
                                  select new ChannelMessageReactionDto(
                                      g.Key,
                                      g.Count(),
                                      g.Select(x => x.reaction.UserId).ToList(),
                                      g.Select(x => x.reactionUser.FullName).ToList(),
                                      g.Select(x => x.reactionUser.AvatarUrl).ToList()
                                  )).ToListAsync(cancellationToken);

            // Calculate Status based on ReadByCount and TotalMemberCount
            MessageStatus status;
            if (result.TotalMemberCount == 0)
                status = MessageStatus.Sent;
            else if (result.ReadByCount >= result.TotalMemberCount)
                status = MessageStatus.Read;
            else if (result.ReadByCount > 0)
                status = MessageStatus.Delivered;
            else
                status = MessageStatus.Sent;

            return new ChannelMessageDto(
                result.Id,
                result.ChannelId,
                result.SenderId,
                result.Email,
                result.FullName,
                result.AvatarUrl,
                result.IsDeleted ? "This message was deleted" : result.Content, // SECURITY: Sanitize deleted content
                result.FileId,
                result.FileName,
                result.FileContentType,
                result.FileSizeInBytes,
                ConvertToFileUrl(result.FileStoragePath),      // FileUrl
                ConvertToThumbnailUrl(result.FileThumbnailPath), // ThumbnailUrl
                result.IsEdited,
                result.IsDeleted,
                result.IsPinned,
                reactions.Sum(r => r.Count), // ReactionCount from loaded reactions
                result.CreatedAtUtc,
                result.EditedAtUtc,
                result.PinnedAtUtc,
                result.ReplyToMessageId,
                result.ReplyToIsDeleted ? "This message was deleted" : result.ReplyToContent, // SECURITY: Sanitize deleted reply content
                result.ReplyToSenderName,
                result.ReplyToFileId,
                result.ReplyToFileName,
                result.ReplyToFileContentType,
                ConvertToFileUrl(result.ReplyToFileStoragePath),      // ReplyToFileUrl
                ConvertToThumbnailUrl(result.ReplyToFileThumbnailPath), // ReplyToThumbnailUrl
                result.IsForwarded,
                result.ReadByCount,
                result.TotalMemberCount,
                result.ReadBy,
                reactions.Count > 0 ? reactions : null,
                mentions.Count > 0 ? mentions : null,
                status
            );
        }

        public async Task<List<ChannelMessageDto>> GetChannelMessagesAsync(
            Guid channelId,
            int pageSize = 30,
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
                        join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                        from file in fileJoin.DefaultIfEmpty()
                        join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                        from repliedFile in repliedFileJoin.DefaultIfEmpty()
                        where message.ChannelId == channelId // Removed IsDeleted filter - show deleted messages as "This message was deleted"
                        select new
                        {
                            message.Id,
                            message.ChannelId,
                            message.SenderId,
                            user.Email,
                            user.FullName,
                            user.AvatarUrl,
                            message.Content,
                            message.FileId,
                            FileName = file != null ? file.OriginalFileName : null,
                            FileContentType = file != null ? file.ContentType : null,
                            FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                            FileStoragePath = file != null ? file.StoragePath : null,
                            FileThumbnailPath = file != null ? file.ThumbnailPath : null,
                            message.IsEdited,
                            message.IsDeleted,
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.PinnedAtUtc,
                            // REMOVED: ReactionCount N+1 query - now batched
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                            ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                            ReplyToSenderName = repliedSender != null ? repliedSender.FullName : null,
                            ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                            ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                            ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
                            ReplyToFileStoragePath = repliedFile != null ? repliedFile.StoragePath : null,
                            ReplyToFileThumbnailPath = repliedFile != null ? repliedFile.ThumbnailPath : null,
                            message.IsForwarded,
                            // REMOVED: ReadByCount and TotalMemberCount N+1 queries - now batched
                            // Get list of users who have read this message from ChannelMessageRead table
                            ReadBy = _context.ChannelMessageReads
                                .Where(r => r.MessageId == message.Id && r.UserId != message.SenderId)
                                .Select(r => r.UserId)
                                .ToList(),
                            // Get reactions grouped by emoji with user details
                            Reactions = (from reaction in _context.ChannelMessageReactions
                                        join reactionUser in _context.Set<UserReadModel>() on reaction.UserId equals reactionUser.Id
                                        where reaction.MessageId == message.Id
                                        group new { reaction, reactionUser } by reaction.Reaction into g
                                        select new ChannelMessageReactionDto(
                                            g.Key,
                                            g.Count(),
                                            g.Select(x => x.reaction.UserId).ToList(),
                                            g.Select(x => x.reactionUser.FullName).ToList(),
                                            g.Select(x => x.reactionUser.AvatarUrl).ToList()
                                        ))
                                .ToList()
                        };

            if (beforeUtc.HasValue)
            {
                query = query.Where(m => m.CreatedAtUtc < beforeUtc.Value);
            }

            var results = await query
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // PERFORMANCE FIX: Batch load counts to eliminate N+1 queries (was 3 subqueries per message)
            var messageIds = results.Select(r => r.Id).ToList();

            // TotalMemberCount is same for all messages in channel (count once, not N times)
            var totalMemberCount = await _context.ChannelMembers
                .Where(m => m.ChannelId == channelId && m.IsActive)
                .CountAsync(cancellationToken) - 1; // Exclude sender

            // Batch load reaction counts
            var reactionCounts = await _context.ChannelMessageReactions
                .Where(r => messageIds.Contains(r.MessageId))
                .GroupBy(r => r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            // Batch load read counts (exclude sender from read count)
            var readCounts = await _context.ChannelMessageReads
                .Where(r => messageIds.Contains(r.MessageId))
                .Join(_context.ChannelMessages, r => r.MessageId, m => m.Id, (r, m) => new { r, m })
                .Where(x => x.r.UserId != x.m.SenderId)
                .GroupBy(x => x.r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            // Load mentions grouped by message
            var mentions = await _context.ChannelMessageMentions
                .Where(m => messageIds.Contains(m.MessageId))
                .GroupBy(m => m.MessageId)
                .Select(g => new
                {
                    MessageId = g.Key,
                    Mentions = g.Select(m => new ChannelMessageMentionDto(m.MentionedUserId, m.MentionedUserFullName, m.IsAllMention)).ToList()
                })
                .ToDictionaryAsync(x => x.MessageId, x => x.Mentions, cancellationToken);

            return results.Select(r => {
                // Get batched counts
                var readByCount = readCounts.TryGetValue(r.Id, out var rbc) ? rbc : 0;
                var reactionCount = reactionCounts.TryGetValue(r.Id, out var rc) ? rc : 0;

                // Calculate Status based on ReadByCount and TotalMemberCount
                MessageStatus status;
                if (totalMemberCount == 0)
                    status = MessageStatus.Sent; // No other members
                else if (readByCount >= totalMemberCount)
                    status = MessageStatus.Read; // Everyone read
                else if (readByCount > 0)
                    status = MessageStatus.Delivered; // At least one person read
                else
                    status = MessageStatus.Sent; // No one read yet

                return new ChannelMessageDto(
                    r.Id,
                    r.ChannelId,
                    r.SenderId,
                    r.Email,
                    r.FullName,
                    r.AvatarUrl,
                    r.IsDeleted ? "This message was deleted" : r.Content, // SECURITY: Sanitize deleted content
                    r.FileId,
                    r.FileName,
                    r.FileContentType,
                    r.FileSizeInBytes,
                    ConvertToFileUrl(r.FileStoragePath),      // FileUrl
                    ConvertToThumbnailUrl(r.FileThumbnailPath), // ThumbnailUrl
                    r.IsEdited,
                    r.IsDeleted,
                    r.IsPinned,
                    reactionCount,
                    r.CreatedAtUtc,
                    r.EditedAtUtc,
                    r.PinnedAtUtc,
                    r.ReplyToMessageId,
                    r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent, // SECURITY: Sanitize deleted reply content
                    r.ReplyToSenderName,
                    r.ReplyToFileId,
                    r.ReplyToFileName,
                    r.ReplyToFileContentType,
                    ConvertToFileUrl(r.ReplyToFileStoragePath),      // ReplyToFileUrl
                    ConvertToThumbnailUrl(r.ReplyToFileThumbnailPath), // ReplyToThumbnailUrl
                    r.IsForwarded,
                    readByCount,
                    totalMemberCount,
                    r.ReadBy, // Include the ReadBy list for real-time read receipt updates
                    r.Reactions, // Include reactions grouped by emoji
                    mentions.TryGetValue(r.Id, out List<ChannelMessageMentionDto>? value) ? value : null, // Include mentions from dictionary
                    status // Set Status based on read receipts
                );
            }).ToList();
        }

        public async Task<List<ChannelMessageDto>> GetMessagesAroundAsync(
            Guid channelId,
            Guid messageId,
            int count = 50,
            CancellationToken cancellationToken = default)
        {
            // 1. Hədəf mesajın tarixini tap
            var targetMessage = await _context.ChannelMessages
                .Where(m => m.Id == messageId && m.ChannelId == channelId)
                .Select(m => new { m.CreatedAtUtc })
                .FirstOrDefaultAsync(cancellationToken);

            if (targetMessage == null)
                return new List<ChannelMessageDto>();

            var targetDate = targetMessage.CreatedAtUtc;
            var halfCount = count / 2;

            // 2. Base query (projection)
            var baseQuery = from message in _context.ChannelMessages
                           join user in _context.Set<UserReadModel>() on message.SenderId equals user.Id
                           join repliedMessage in _context.ChannelMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                           from repliedMessage in replyJoin.DefaultIfEmpty()
                           join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                           from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                           join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                           from file in fileJoin.DefaultIfEmpty()
                           join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                           from repliedFile in repliedFileJoin.DefaultIfEmpty()
                           where message.ChannelId == channelId
                           select new
                           {
                               message.Id,
                               message.ChannelId,
                               message.SenderId,
                               user.Email,
                               user.FullName,
                               user.AvatarUrl,
                               message.Content,
                               message.FileId,
                               FileName = file != null ? file.OriginalFileName : null,
                               FileContentType = file != null ? file.ContentType : null,
                               FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                               FileStoragePath = file != null ? file.StoragePath : null,
                               FileThumbnailPath = file != null ? file.ThumbnailPath : null,
                               message.IsEdited,
                               message.IsDeleted,
                               message.IsPinned,
                               message.CreatedAtUtc,
                               message.EditedAtUtc,
                               message.PinnedAtUtc,
                               // REMOVED: ReactionCount N+1 query - now batched
                               message.ReplyToMessageId,
                               ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                               ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                               ReplyToSenderName = repliedSender != null ? repliedSender.FullName : null,
                               ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                               ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                               ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
                               ReplyToFileStoragePath = repliedFile != null ? repliedFile.StoragePath : null,
                               ReplyToFileThumbnailPath = repliedFile != null ? repliedFile.ThumbnailPath : null,
                               message.IsForwarded,
                               // REMOVED: ReadByCount and TotalMemberCount N+1 queries - now batched
                               ReadBy = _context.ChannelMessageReads
                                   .Where(r => r.MessageId == message.Id && r.UserId != message.SenderId)
                                   .Select(r => r.UserId)
                                   .ToList(),
                               Reactions = (from reaction in _context.ChannelMessageReactions
                                           join reactionUser in _context.Set<UserReadModel>() on reaction.UserId equals reactionUser.Id
                                           where reaction.MessageId == message.Id
                                           group new { reaction, reactionUser } by reaction.Reaction into g
                                           select new ChannelMessageReactionDto(
                                               g.Key,
                                               g.Count(),
                                               g.Select(x => x.reaction.UserId).ToList(),
                                               g.Select(x => x.reactionUser.FullName).ToList(),
                                               g.Select(x => x.reactionUser.AvatarUrl).ToList()
                                           )).ToList()
                           };

            // 3. Hədəf mesajdan ƏVVƏL olan mesajlar (hədəf daxil)
            var beforeMessages = await baseQuery
                .Where(m => m.CreatedAtUtc <= targetDate)
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(halfCount + 1) // +1 hədəf mesajın özü üçün
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

            // PERFORMANCE FIX: Batch load counts to eliminate N+1 queries (was 3 subqueries per message)
            var messageIds = results.Select(r => r.Id).ToList();

            // TotalMemberCount is same for all messages in channel (count once, not N times)
            var totalMemberCount = await _context.ChannelMembers
                .Where(m => m.ChannelId == channelId && m.IsActive)
                .CountAsync(cancellationToken) - 1; // Exclude sender

            // Batch load reaction counts
            var reactionCounts = await _context.ChannelMessageReactions
                .Where(r => messageIds.Contains(r.MessageId))
                .GroupBy(r => r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            // Batch load read counts (exclude sender from read count)
            var readCounts = await _context.ChannelMessageReads
                .Where(r => messageIds.Contains(r.MessageId))
                .Join(_context.ChannelMessages, r => r.MessageId, m => m.Id, (r, m) => new { r, m })
                .Where(x => x.r.UserId != x.m.SenderId)
                .GroupBy(x => x.r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            // Load mentions grouped by message
            var mentions = await _context.ChannelMessageMentions
                .Where(m => messageIds.Contains(m.MessageId))
                .GroupBy(m => m.MessageId)
                .Select(g => new
                {
                    MessageId = g.Key,
                    Mentions = g.Select(m => new ChannelMessageMentionDto(m.MentionedUserId, m.MentionedUserFullName, m.IsAllMention)).ToList()
                })
                .ToDictionaryAsync(x => x.MessageId, x => x.Mentions, cancellationToken);

            return results.Select(r => {
                // Get batched counts
                var readByCount = readCounts.TryGetValue(r.Id, out var rbc) ? rbc : 0;
                var reactionCount = reactionCounts.TryGetValue(r.Id, out var rc) ? rc : 0;

                // Calculate Status based on ReadByCount and TotalMemberCount
                MessageStatus status;
                if (totalMemberCount == 0)
                    status = MessageStatus.Sent;
                else if (readByCount >= totalMemberCount)
                    status = MessageStatus.Read;
                else if (readByCount > 0)
                    status = MessageStatus.Delivered;
                else
                    status = MessageStatus.Sent;

                return new ChannelMessageDto(
                    r.Id,
                    r.ChannelId,
                    r.SenderId,
                    r.Email,
                    r.FullName,
                    r.AvatarUrl,
                    r.IsDeleted ? "This message was deleted" : r.Content,
                    r.FileId,
                    r.FileName,
                    r.FileContentType,
                    r.FileSizeInBytes,
                    ConvertToFileUrl(r.FileStoragePath),      // FileUrl
                    ConvertToThumbnailUrl(r.FileThumbnailPath), // ThumbnailUrl
                    r.IsEdited,
                    r.IsDeleted,
                    r.IsPinned,
                    reactionCount,
                    r.CreatedAtUtc,
                    r.EditedAtUtc,
                    r.PinnedAtUtc,
                    r.ReplyToMessageId,
                    r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent,
                    r.ReplyToSenderName,
                    r.ReplyToFileId,
                    r.ReplyToFileName,
                    r.ReplyToFileContentType,
                    ConvertToFileUrl(r.ReplyToFileStoragePath),      // ReplyToFileUrl
                    ConvertToThumbnailUrl(r.ReplyToFileThumbnailPath), // ReplyToThumbnailUrl
                    r.IsForwarded,
                    readByCount,
                    totalMemberCount,
                    r.ReadBy,
                    r.Reactions,
                    mentions.ContainsKey(r.Id) ? mentions[r.Id] : null,
                    status
                );
            }).ToList();
        }

        public async Task<List<ChannelMessageDto>> GetMessagesBeforeDateAsync(
            Guid channelId,
            DateTime beforeUtc,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            // Base query with all joins
            var query = from message in _context.ChannelMessages
                        join user in _context.Set<UserReadModel>() on message.SenderId equals user.Id
                        join repliedMessage in _context.ChannelMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                        from repliedMessage in replyJoin.DefaultIfEmpty()
                        join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                        from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                        join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                        from file in fileJoin.DefaultIfEmpty()
                        join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                        from repliedFile in repliedFileJoin.DefaultIfEmpty()
                        where message.ChannelId == channelId
                           && message.CreatedAtUtc < beforeUtc
                        select new
                        {
                            message.Id,
                            message.ChannelId,
                            message.SenderId,
                            user.Email,
                            user.FullName,
                            user.AvatarUrl,
                            message.Content,
                            message.FileId,
                            FileName = file != null ? file.OriginalFileName : null,
                            FileContentType = file != null ? file.ContentType : null,
                            FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                            FileStoragePath = file != null ? file.StoragePath : null,
                            FileThumbnailPath = file != null ? file.ThumbnailPath : null,
                            message.IsEdited,
                            message.IsDeleted,
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.PinnedAtUtc,
                            // REMOVED: ReactionCount N+1 query - now batched
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                            ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                            ReplyToSenderName = repliedSender != null ? repliedSender.FullName : null,
                            ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                            ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                            ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
                            ReplyToFileStoragePath = repliedFile != null ? repliedFile.StoragePath : null,
                            ReplyToFileThumbnailPath = repliedFile != null ? repliedFile.ThumbnailPath : null,
                            message.IsForwarded,
                            // REMOVED: ReadByCount and TotalMemberCount N+1 queries - now batched
                            ReadBy = _context.ChannelMessageReads
                                .Where(r => r.MessageId == message.Id && r.UserId != message.SenderId)
                                .Select(r => r.UserId)
                                .ToList(),
                            Reactions = (from reaction in _context.ChannelMessageReactions
                                        join reactionUser in _context.Set<UserReadModel>() on reaction.UserId equals reactionUser.Id
                                        where reaction.MessageId == message.Id
                                        group new { reaction, reactionUser } by reaction.Reaction into g
                                        select new ChannelMessageReactionDto(
                                            g.Key,
                                            g.Count(),
                                            g.Select(x => x.reaction.UserId).ToList(),
                                            g.Select(x => x.reactionUser.FullName).ToList(),
                                            g.Select(x => x.reactionUser.AvatarUrl).ToList()
                                        ))
                                .ToList()
                        };

            var results = await query
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(limit)
                .ToListAsync(cancellationToken);

            // PERFORMANCE FIX: Batch load counts to eliminate N+1 queries (was 3 subqueries per message)
            var messageIds = results.Select(r => r.Id).ToList();

            // TotalMemberCount is same for all messages in channel (count once, not N times)
            var totalMemberCount = await _context.ChannelMembers
                .Where(m => m.ChannelId == channelId && m.IsActive)
                .CountAsync(cancellationToken) - 1; // Exclude sender

            // Batch load reaction counts
            var reactionCounts = await _context.ChannelMessageReactions
                .Where(r => messageIds.Contains(r.MessageId))
                .GroupBy(r => r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            // Batch load read counts (exclude sender from read count)
            var readCounts = await _context.ChannelMessageReads
                .Where(r => messageIds.Contains(r.MessageId))
                .Join(_context.ChannelMessages, r => r.MessageId, m => m.Id, (r, m) => new { r, m })
                .Where(x => x.r.UserId != x.m.SenderId)
                .GroupBy(x => x.r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            // Load mentions grouped by message
            var mentions = await _context.ChannelMessageMentions
                .Where(m => messageIds.Contains(m.MessageId))
                .GroupBy(m => m.MessageId)
                .Select(g => new
                {
                    MessageId = g.Key,
                    Mentions = g.Select(m => new ChannelMessageMentionDto(m.MentionedUserId, m.MentionedUserFullName, m.IsAllMention)).ToList()
                })
                .ToDictionaryAsync(x => x.MessageId, x => x.Mentions, cancellationToken);

            return results.Select(r => {
                // Get batched counts
                var readByCount = readCounts.TryGetValue(r.Id, out var rbc) ? rbc : 0;
                var reactionCount = reactionCounts.TryGetValue(r.Id, out var rc) ? rc : 0;

                // Calculate Status based on ReadByCount and TotalMemberCount
                MessageStatus status;
                if (totalMemberCount == 0)
                    status = MessageStatus.Sent;
                else if (readByCount >= totalMemberCount)
                    status = MessageStatus.Read;
                else if (readByCount > 0)
                    status = MessageStatus.Delivered;
                else
                    status = MessageStatus.Sent;

                return new ChannelMessageDto(
                    r.Id,
                    r.ChannelId,
                    r.SenderId,
                    r.Email,
                    r.FullName,
                    r.AvatarUrl,
                    r.IsDeleted ? "This message was deleted" : r.Content,
                    r.FileId,
                    r.FileName,
                    r.FileContentType,
                    r.FileSizeInBytes,
                    ConvertToFileUrl(r.FileStoragePath),      // FileUrl
                    ConvertToThumbnailUrl(r.FileThumbnailPath), // ThumbnailUrl
                    r.IsEdited,
                    r.IsDeleted,
                    r.IsPinned,
                    reactionCount,
                    r.CreatedAtUtc,
                    r.EditedAtUtc,
                    r.PinnedAtUtc,
                    r.ReplyToMessageId,
                    r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent,
                    r.ReplyToSenderName,
                    r.ReplyToFileId,
                    r.ReplyToFileName,
                    r.ReplyToFileContentType,
                    ConvertToFileUrl(r.ReplyToFileStoragePath),      // ReplyToFileUrl
                    ConvertToThumbnailUrl(r.ReplyToFileThumbnailPath), // ReplyToThumbnailUrl
                    r.IsForwarded,
                    readByCount,
                    totalMemberCount,
                    r.ReadBy,
                    r.Reactions,
                    mentions.TryGetValue(r.Id, out List<ChannelMessageMentionDto>? value) ? value : null,
                    status
                );
            }).ToList();
        }

        public async Task<List<ChannelMessageDto>> GetMessagesAfterDateAsync(
            Guid channelId,
            DateTime afterUtc,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            // Base query with all joins
            var query = from message in _context.ChannelMessages
                        join user in _context.Set<UserReadModel>() on message.SenderId equals user.Id
                        join repliedMessage in _context.ChannelMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                        from repliedMessage in replyJoin.DefaultIfEmpty()
                        join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                        from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                        join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                        from file in fileJoin.DefaultIfEmpty()
                        join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                        from repliedFile in repliedFileJoin.DefaultIfEmpty()
                        where message.ChannelId == channelId
                           && message.CreatedAtUtc > afterUtc
                        select new
                        {
                            message.Id,
                            message.ChannelId,
                            message.SenderId,
                            user.Email,
                            user.FullName,
                            user.AvatarUrl,
                            message.Content,
                            message.FileId,
                            FileName = file != null ? file.OriginalFileName : null,
                            FileContentType = file != null ? file.ContentType : null,
                            FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                            FileStoragePath = file != null ? file.StoragePath : null,
                            FileThumbnailPath = file != null ? file.ThumbnailPath : null,
                            message.IsEdited,
                            message.IsDeleted,
                            message.IsPinned,
                            message.CreatedAtUtc,
                            message.EditedAtUtc,
                            message.PinnedAtUtc,
                            // REMOVED: ReactionCount N+1 query - now batched
                            message.ReplyToMessageId,
                            ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : null,
                            ReplyToIsDeleted = repliedMessage != null && repliedMessage.IsDeleted,
                            ReplyToSenderName = repliedSender != null ? repliedSender.FullName : null,
                            ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                            ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                            ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
                            ReplyToFileStoragePath = repliedFile != null ? repliedFile.StoragePath : null,
                            ReplyToFileThumbnailPath = repliedFile != null ? repliedFile.ThumbnailPath : null,
                            message.IsForwarded,
                            // REMOVED: ReadByCount and TotalMemberCount N+1 queries - now batched
                            ReadBy = _context.ChannelMessageReads
                                .Where(r => r.MessageId == message.Id && r.UserId != message.SenderId)
                                .Select(r => r.UserId)
                                .ToList(),
                            Reactions = (from reaction in _context.ChannelMessageReactions
                                        join reactionUser in _context.Set<UserReadModel>() on reaction.UserId equals reactionUser.Id
                                        where reaction.MessageId == message.Id
                                        group new { reaction, reactionUser } by reaction.Reaction into g
                                        select new ChannelMessageReactionDto(
                                            g.Key,
                                            g.Count(),
                                            g.Select(x => x.reaction.UserId).ToList(),
                                            g.Select(x => x.reactionUser.FullName).ToList(),
                                            g.Select(x => x.reactionUser.AvatarUrl).ToList()
                                        ))
                                .ToList()
                        };

            var results = await query
                .OrderBy(m => m.CreatedAtUtc)
                .Take(limit)
                .ToListAsync(cancellationToken);

            // PERFORMANCE FIX: Batch load counts to eliminate N+1 queries (was 3 subqueries per message)
            var messageIds = results.Select(r => r.Id).ToList();

            // TotalMemberCount is same for all messages in channel (count once, not N times)
            var totalMemberCount = await _context.ChannelMembers
                .Where(m => m.ChannelId == channelId && m.IsActive)
                .CountAsync(cancellationToken) - 1; // Exclude sender

            // Batch load reaction counts
            var reactionCounts = await _context.ChannelMessageReactions
                .Where(r => messageIds.Contains(r.MessageId))
                .GroupBy(r => r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            // Batch load read counts (exclude sender from read count)
            var readCounts = await _context.ChannelMessageReads
                .Where(r => messageIds.Contains(r.MessageId))
                .Join(_context.ChannelMessages, r => r.MessageId, m => m.Id, (r, m) => new { r, m })
                .Where(x => x.r.UserId != x.m.SenderId)
                .GroupBy(x => x.r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            // Load mentions grouped by message
            var mentions = await _context.ChannelMessageMentions
                .Where(m => messageIds.Contains(m.MessageId))
                .GroupBy(m => m.MessageId)
                .Select(g => new
                {
                    MessageId = g.Key,
                    Mentions = g.Select(m => new ChannelMessageMentionDto(m.MentionedUserId, m.MentionedUserFullName, m.IsAllMention)).ToList()
                })
                .ToDictionaryAsync(x => x.MessageId, x => x.Mentions, cancellationToken);

            return results.Select(r => {
                // Get batched counts
                var readByCount = readCounts.TryGetValue(r.Id, out var rbc) ? rbc : 0;
                var reactionCount = reactionCounts.TryGetValue(r.Id, out var rc) ? rc : 0;

                // Calculate Status based on ReadByCount and TotalMemberCount
                MessageStatus status;
                if (totalMemberCount == 0)
                    status = MessageStatus.Sent;
                else if (readByCount >= totalMemberCount)
                    status = MessageStatus.Read;
                else if (readByCount > 0)
                    status = MessageStatus.Delivered;
                else
                    status = MessageStatus.Sent;

                return new ChannelMessageDto(
                    r.Id,
                    r.ChannelId,
                    r.SenderId,
                    r.Email,
                    r.FullName,
                    r.AvatarUrl,
                    r.IsDeleted ? "This message was deleted" : r.Content,
                    r.FileId,
                    r.FileName,
                    r.FileContentType,
                    r.FileSizeInBytes,
                    ConvertToFileUrl(r.FileStoragePath),      // FileUrl
                    ConvertToThumbnailUrl(r.FileThumbnailPath), // ThumbnailUrl
                    r.IsEdited,
                    r.IsDeleted,
                    r.IsPinned,
                    reactionCount,
                    r.CreatedAtUtc,
                    r.EditedAtUtc,
                    r.PinnedAtUtc,
                    r.ReplyToMessageId,
                    r.ReplyToIsDeleted ? "This message was deleted" : r.ReplyToContent,
                    r.ReplyToSenderName,
                    r.ReplyToFileId,
                    r.ReplyToFileName,
                    r.ReplyToFileContentType,
                    ConvertToFileUrl(r.ReplyToFileStoragePath),      // ReplyToFileUrl
                    ConvertToThumbnailUrl(r.ReplyToFileThumbnailPath), // ReplyToThumbnailUrl
                    r.IsForwarded,
                    readByCount,
                    totalMemberCount,
                    r.ReadBy,
                    r.Reactions,
                    mentions.TryGetValue(r.Id, out List<ChannelMessageMentionDto>? value) ? value : null,
                    status
                );
            }).ToList();
        }

        public async Task<List<ChannelMessageDto>> GetPinnedMessagesAsync(Guid channelId, CancellationToken cancellationToken = default)
        {
            // Step 1: Get pinned messages with joins (no per-message subqueries)
            var results = await (from message in _context.ChannelMessages
                          join user in _context.Set<UserReadModel>() on message.SenderId equals user.Id
                          join repliedMessage in _context.ChannelMessages on message.ReplyToMessageId equals repliedMessage.Id into replyJoin
                          from repliedMessage in replyJoin.DefaultIfEmpty()
                          join repliedSender in _context.Set<UserReadModel>() on repliedMessage.SenderId equals repliedSender.Id into repliedSenderJoin
                          from repliedSender in repliedSenderJoin.DefaultIfEmpty()
                          join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on message.FileId equals file.Id.ToString() into fileJoin
                          from file in fileJoin.DefaultIfEmpty()
                          join repliedFile in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>() on repliedMessage.FileId equals repliedFile.Id.ToString() into repliedFileJoin
                          from repliedFile in repliedFileJoin.DefaultIfEmpty()
                          where message.ChannelId == channelId
                             && message.IsPinned
                             && !message.IsDeleted
                          orderby message.PinnedAtUtc ascending
                          select new
                          {
                              message.Id,
                              message.ChannelId,
                              message.SenderId,
                              user.Email,
                              user.FullName,
                              user.AvatarUrl,
                              message.Content,
                              message.FileId,
                              FileName = file != null ? file.OriginalFileName : null,
                              FileContentType = file != null ? file.ContentType : null,
                              FileSizeInBytes = file != null ? (long?)file.FileSizeInBytes : null,
                              FileStoragePath = file != null ? file.StoragePath : null,
                              FileThumbnailPath = file != null ? file.ThumbnailPath : null,
                              message.IsEdited,
                              message.IsDeleted,
                              message.IsPinned,
                              message.CreatedAtUtc,
                              message.EditedAtUtc,
                              message.PinnedAtUtc,
                              message.ReplyToMessageId,
                              ReplyToContent = repliedMessage != null && !repliedMessage.IsDeleted ? repliedMessage.Content : "This message was deleted",
                              ReplyToSenderName = repliedSender != null ? repliedSender.FullName : null,
                              ReplyToFileId = repliedMessage != null ? repliedMessage.FileId : null,
                              ReplyToFileName = repliedFile != null ? repliedFile.OriginalFileName : null,
                              ReplyToFileContentType = repliedFile != null ? repliedFile.ContentType : null,
                              ReplyToFileStoragePath = repliedFile != null ? repliedFile.StoragePath : null,
                              ReplyToFileThumbnailPath = repliedFile != null ? repliedFile.ThumbnailPath : null,
                              message.IsForwarded
                          }).ToListAsync(cancellationToken);

            if (results.Count == 0)
                return new List<ChannelMessageDto>();

            // Step 2: Batch load counts to eliminate N+1 queries
            var messageIds = results.Select(r => r.Id).ToList();

            var totalMemberCount = await _context.ChannelMembers
                .Where(m => m.ChannelId == channelId && m.IsActive)
                .CountAsync(cancellationToken) - 1;

            var reactionCounts = await _context.ChannelMessageReactions
                .Where(r => messageIds.Contains(r.MessageId))
                .GroupBy(r => r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            var readCounts = await _context.ChannelMessageReads
                .Where(r => messageIds.Contains(r.MessageId))
                .Join(_context.ChannelMessages, r => r.MessageId, m => m.Id, (r, m) => new { r, m })
                .Where(x => x.r.UserId != x.m.SenderId)
                .GroupBy(x => x.r.MessageId)
                .Select(g => new { MessageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);

            var readByUsers = await _context.ChannelMessageReads
                .Where(r => messageIds.Contains(r.MessageId))
                .Join(_context.ChannelMessages, r => r.MessageId, m => m.Id, (r, m) => new { r, m })
                .Where(x => x.r.UserId != x.m.SenderId)
                .GroupBy(x => x.r.MessageId)
                .Select(g => new { MessageId = g.Key, UserIds = g.Select(x => x.r.UserId).ToList() })
                .ToDictionaryAsync(x => x.MessageId, x => x.UserIds, cancellationToken);

            var reactions = await (from reaction in _context.ChannelMessageReactions
                                  join reactionUser in _context.Set<UserReadModel>() on reaction.UserId equals reactionUser.Id
                                  where messageIds.Contains(reaction.MessageId)
                                  select new { reaction.MessageId, reaction.Reaction, reaction.UserId, reactionUser.FullName, reactionUser.AvatarUrl })
                                  .ToListAsync(cancellationToken);

            var reactionsByMessage = reactions
                .GroupBy(r => r.MessageId)
                .ToDictionary(g => g.Key, g => g
                    .GroupBy(r => r.Reaction)
                    .Select(rg => new ChannelMessageReactionDto(
                        rg.Key,
                        rg.Count(),
                        rg.Select(x => x.UserId).ToList(),
                        rg.Select(x => x.FullName).ToList(),
                        rg.Select(x => x.AvatarUrl).ToList()))
                    .ToList());

            var mentions = await _context.ChannelMessageMentions
                .Where(m => messageIds.Contains(m.MessageId))
                .GroupBy(m => m.MessageId)
                .Select(g => new { MessageId = g.Key, Mentions = g.Select(m => new ChannelMessageMentionDto(m.MentionedUserId, m.MentionedUserFullName, m.IsAllMention)).ToList() })
                .ToDictionaryAsync(x => x.MessageId, x => x.Mentions, cancellationToken);

            // Step 3: Map to DTOs
            return results.Select(r =>
            {
                var readByCount = readCounts.TryGetValue(r.Id, out var rbc) ? rbc : 0;
                var reactionCount = reactionCounts.TryGetValue(r.Id, out var rc) ? rc : 0;

                MessageStatus status;
                if (totalMemberCount == 0)
                    status = MessageStatus.Sent;
                else if (readByCount >= totalMemberCount)
                    status = MessageStatus.Read;
                else if (readByCount > 0)
                    status = MessageStatus.Delivered;
                else
                    status = MessageStatus.Sent;

                return new ChannelMessageDto(
                    r.Id, r.ChannelId, r.SenderId, r.Email, r.FullName, r.AvatarUrl,
                    r.Content,
                    r.FileId, r.FileName, r.FileContentType, r.FileSizeInBytes,
                    ConvertToFileUrl(r.FileStoragePath),      // FileUrl
                    ConvertToThumbnailUrl(r.FileThumbnailPath), // ThumbnailUrl
                    r.IsEdited, r.IsDeleted, r.IsPinned,
                    reactionCount, r.CreatedAtUtc, r.EditedAtUtc, r.PinnedAtUtc,
                    r.ReplyToMessageId, r.ReplyToContent, r.ReplyToSenderName,
                    r.ReplyToFileId, r.ReplyToFileName, r.ReplyToFileContentType,
                    ConvertToFileUrl(r.ReplyToFileStoragePath),      // ReplyToFileUrl
                    ConvertToThumbnailUrl(r.ReplyToFileThumbnailPath), // ReplyToThumbnailUrl
                    r.IsForwarded, readByCount, totalMemberCount,
                    readByUsers.TryGetValue(r.Id, out var rbu) ? rbu : new List<Guid>(),
                    reactionsByMessage.TryGetValue(r.Id, out var rxns) ? rxns : null,
                    mentions.TryGetValue(r.Id, out var mns) ? mns : null,
                    status
                );
            }).ToList();
        }

        public async Task<int> GetUnreadCountAsync(Guid channelId, Guid userId, CancellationToken cancellationToken = default)
        {
            // Verify user is a member of the channel
            var member = await _context.ChannelMembers
                .Where(m => m.ChannelId == channelId && m.UserId == userId && m.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
                return 0;

            // Count messages in the channel that:
            // 1. Are not deleted
            // 2. Were not sent by the user
            // 3. Don't have a corresponding ChannelMessageRead record for this user
            return await _context.ChannelMessages
                .Where(m => m.ChannelId == channelId
                         && !m.IsDeleted
                         && m.SenderId != userId
                         && !_context.ChannelMessageReads.Any(r => r.MessageId == m.Id && r.UserId == userId))
                .CountAsync(cancellationToken);
        }

        public async Task<int> MarkAllAsReadAsync(Guid channelId, Guid userId, CancellationToken cancellationToken = default)
        {
            // Get all unread message IDs in the channel (excluding user's own messages)
            var unreadMessageIds = await _context.ChannelMessages
                .Where(m => m.ChannelId == channelId
                         && !m.IsDeleted
                         && m.SenderId != userId
                         && !_context.ChannelMessageReads.Any(r => r.MessageId == m.Id && r.UserId == userId))
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            if (!unreadMessageIds.Any())
                return 0;

            // Bulk insert read records for all unread messages
            var readRecords = unreadMessageIds.Select(msgId => new ChannelMessageRead(msgId, userId)).ToList();

            await _context.ChannelMessageReads.AddRangeAsync(readRecords, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return readRecords.Count;
        }

        public async Task<bool> HasMessagesAsync(Guid channelId, CancellationToken cancellationToken = default)
        {
            return await _context.ChannelMessages
                .AnyAsync(m => m.ChannelId == channelId, cancellationToken);
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

        #region File URL Helpers

        /// <summary>
        /// StoragePath-i statik URL-ə çevirir.
        /// </summary>
        private static string? ConvertToFileUrl(string? storagePath)
        {
            if (string.IsNullOrEmpty(storagePath))
                return null;

            return $"/uploads/{storagePath}";
        }

        /// <summary>
        /// ThumbnailPath-i statik URL-ə çevirir.
        /// </summary>
        private static string? ConvertToThumbnailUrl(string? thumbnailPath)
        {
            if (string.IsNullOrEmpty(thumbnailPath))
                return null;

            return $"/uploads/{thumbnailPath}";
        }

        #endregion
    }
}