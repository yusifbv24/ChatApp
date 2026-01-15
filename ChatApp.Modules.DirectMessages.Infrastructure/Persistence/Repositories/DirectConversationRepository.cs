using ChatApp.Modules.DirectMessages.Application.DTOs.Request;
using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Repositories
{
    public class DirectConversationRepository(DirectMessagesDbContext context) : IDirectConversationRepository
    {
        private readonly DirectMessagesDbContext _context = context;

        public async Task AddAsync(DirectConversation conversation, CancellationToken cancellationToken = default)
        {
            await _context.DirectConversations.AddAsync(conversation, cancellationToken);
        }


        public Task DeleteAsync(DirectConversation conversation, CancellationToken cancellationToken = default)
        {
            _context.DirectConversations.Remove(conversation);
            return Task.CompletedTask;
        }


        public async Task<bool> ExistsAsync(
            Guid user1Id,
            Guid user2Id, 
            CancellationToken cancellationToken = default)
        {
            var (smallerId, largerId) = user1Id < user2Id ? (user1Id, user2Id) : (user2Id, user1Id);

            return await _context.DirectConversations
                .AnyAsync(c => c.User1Id == smallerId && c.User2Id == largerId, cancellationToken);
        }


        public async Task<DirectConversation?> GetByIdAsync(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            return await _context.DirectConversations
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }


        public async Task<DirectConversation?> GetByParticipantsAsync(
            Guid user1Id, 
            Guid user2Id, 
            CancellationToken cancellationToken = default)
        {
            // Normalize user order for consistent lookup
            var (smallerId, largerId) = user1Id < user2Id ? (user1Id, user2Id) : (user2Id, user1Id);

            return await _context.DirectConversations
                .FirstOrDefaultAsync(
                    c => c.User1Id == smallerId && c.User2Id == largerId,
                    cancellationToken);
        }



        public async Task<List<DirectConversationDto>> GetUserConversationsAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            // Get conversations with other user details and last message
            // Only show conversations where:
            // 1. User is the initiator (can always see their own initiated conversations), OR
            // 2. User is NOT the initiator AND the conversation has messages, OR
            // 3. Notes conversation (always visible)
            var conversationsQuery = await (from conv in _context.DirectConversations
                                       where ((conv.User1Id == userId && conv.IsUser1Active) ||
                                              (conv.User2Id == userId && conv.IsUser2Active))
                                             && (conv.InitiatedByUserId == userId || conv.HasMessages || conv.IsNotes)
                                       let otherUserId = conv.User1Id == userId ? conv.User2Id : conv.User1Id
                                       join user in _context.Set<UserReadModel>() on otherUserId equals user.Id
                                       let lastMessageInfo = (from m in _context.DirectMessages
                                          where m.ConversationId == conv.Id
                                          orderby m.CreatedAtUtc descending
                                          join file in _context.Set<ChatApp.Modules.Files.Domain.Entities.FileMetadata>()
                                              on m.FileId equals file.Id.ToString() into fileGroup
                                          from file in fileGroup.DefaultIfEmpty()
                                          select new { m.Id, m.Content, m.IsDeleted, m.SenderId, m.IsRead, m.FileId, FileContentType = file != null ? file.ContentType : null })
                                          .FirstOrDefault()
                                       let unreadCount = _context.DirectMessages
                                          .Count(m => m.ConversationId == conv.Id &&
                                                    m.ReceiverId == userId &&
                                                    !m.IsRead &&
                                                    !m.IsDeleted)
                                       let lastReadLaterMessageId = conv.User1Id == userId
                                           ? conv.User1LastReadLaterMessageId
                                           : conv.User2LastReadLaterMessageId
                                       orderby conv.LastMessageAtUtc descending
                                       select new
                                       {
                                           conv.Id,
                                           conv.IsNotes,
                                           OtherUserId=otherUserId,
                                           user.Username,
                                           user.DisplayName,
                                           user.AvatarUrl,
                                           LastMessage = lastMessageInfo == null ? null :
                                               lastMessageInfo.IsDeleted ? "This message was deleted" :
                                               lastMessageInfo.FileId != null ?
                                                   (lastMessageInfo.FileContentType != null && lastMessageInfo.FileContentType.StartsWith("image/") ?
                                                       (string.IsNullOrWhiteSpace(lastMessageInfo.Content) ? "[Image]" : "[Image] " + lastMessageInfo.Content) :
                                                       (string.IsNullOrWhiteSpace(lastMessageInfo.Content) ? "[File]" : "[File] " + lastMessageInfo.Content)) :
                                               lastMessageInfo.Content,
                                           conv.LastMessageAtUtc,
                                           UnreadCount=unreadCount,
                                           LastReadLaterMessageId=lastReadLaterMessageId,
                                           LastMessageSenderId = lastMessageInfo != null ? (Guid?)lastMessageInfo.SenderId : null,
                                           LastMessageIsRead = lastMessageInfo != null && lastMessageInfo.IsRead,
                                           LastMessageId = lastMessageInfo != null ? (Guid?)lastMessageInfo.Id : null
                                       })
                                       .ToListAsync(cancellationToken);

            // Batch query: Get ALL unread messages, then find first for each conversation in-memory
            var conversationIds = conversationsQuery.Select(c => c.Id).ToList();
            var firstUnreadMessageIds = new Dictionary<Guid, Guid>();
            var hasUnreadMentionsDictionary = new Dictionary<Guid, bool>();

            if (conversationIds.Any())
            {
                // Simple query - get all unread messages
                var allUnreadMessages = await _context.DirectMessages
                    .Where(m => conversationIds.Contains(m.ConversationId) &&
                                m.ReceiverId == userId &&
                                !m.IsRead &&
                                !m.IsDeleted)
                    .Select(m => new { m.Id, m.ConversationId, m.CreatedAtUtc })
                    .ToListAsync(cancellationToken);

                // In-memory GroupBy and OrderBy - EF Core translation issue workaround
                firstUnreadMessageIds = allUnreadMessages
                    .GroupBy(m => m.ConversationId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(m => m.CreatedAtUtc).First().Id
                    );

                // Batch query: Get conversations with unread mentions
                var conversationsWithMentions = await _context.DirectMessageMentions
                    .Where(mention => conversationIds.Contains(mention.Message.ConversationId) &&
                                      mention.MentionedUserId == userId &&
                                      !mention.Message.IsRead &&
                                      !mention.Message.IsDeleted)
                    .Select(mention => mention.Message.ConversationId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                foreach (var convId in conversationsWithMentions)
                {
                    hasUnreadMentionsDictionary[convId] = true;
                }
            }

            // Map to DTOs with message status
            var conversations = conversationsQuery.Select(c =>
            {
                // Calculate status for user's own messages only
                string? status = null;
                if (c.LastMessageSenderId == userId)
                {
                    status = c.LastMessageIsRead ? "Read" : "Sent";
                }

                return new DirectConversationDto(
                    c.Id,
                    c.OtherUserId,
                    c.Username,
                    c.DisplayName,
                    c.AvatarUrl,
                    c.LastMessage,
                    c.LastMessageAtUtc,
                    c.UnreadCount,
                    hasUnreadMentionsDictionary.ContainsKey(c.Id) && hasUnreadMentionsDictionary[c.Id],
                    c.LastReadLaterMessageId,
                    c.LastMessageSenderId,
                    status,
                    c.LastMessageId,
                    firstUnreadMessageIds.ContainsKey(c.Id) ? (Guid?)firstUnreadMessageIds[c.Id] : null,
                    c.IsNotes
                );
            }).ToList();

            return conversations;
        }



        public Task UpdateAsync(DirectConversation conversation, CancellationToken cancellationToken = default)
        {
            _context.DirectConversations.Update(conversation);
            return Task.CompletedTask;
        }
    }
}