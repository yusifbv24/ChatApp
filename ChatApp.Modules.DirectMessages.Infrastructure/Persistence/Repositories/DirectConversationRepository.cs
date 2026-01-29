using ChatApp.Modules.DirectMessages.Application.DTOs.Request;
using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using ChatApp.Modules.Files.Domain.Entities;
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
                                       join member in _context.DirectConversationMembers
                                           on new { conv.Id, UserId = userId } equals new { Id = member.ConversationId, member.UserId }
                                       where member.IsActive && !member.IsHidden
                                             && (conv.InitiatedByUserId == userId || conv.HasMessages || conv.IsNotes)
                                       let otherUserId = conv.User1Id == userId ? conv.User2Id : conv.User1Id
                                       join user in _context.Set<UserReadModel>() on otherUserId equals user.Id
                                       let lastMessageInfo = (from m in _context.DirectMessages
                                          where m.ConversationId == conv.Id
                                          orderby m.CreatedAtUtc descending
                                          join file in _context.Set<FileMetadata>()
                                              on m.FileId equals file.Id.ToString() into fileGroup
                                          from file in fileGroup.DefaultIfEmpty()
                                          select new { m.Id, m.Content, m.IsDeleted, m.SenderId, m.IsRead, m.FileId, FileContentType = file != null ? file.ContentType : null })
                                          .FirstOrDefault()
                                       orderby conv.LastMessageAtUtc descending
                                       select new
                                       {
                                           conv.Id,
                                           conv.IsNotes,
                                           OtherUserId=otherUserId,
                                           OtherUserEmail = user.Email,
                                           FullName = user.FullName,
                                           user.AvatarUrl,
                                           LastMessage = lastMessageInfo == null ? null :
                                               lastMessageInfo.IsDeleted ? "This message was deleted" :
                                               lastMessageInfo.FileId != null ?
                                                   (lastMessageInfo.FileContentType != null && lastMessageInfo.FileContentType.StartsWith("image/") ?
                                                       (string.IsNullOrWhiteSpace(lastMessageInfo.Content) ? "[Image]" : "[Image] " + lastMessageInfo.Content) :
                                                       (string.IsNullOrWhiteSpace(lastMessageInfo.Content) ? "[File]" : "[File] " + lastMessageInfo.Content)) :
                                               lastMessageInfo.Content,
                                           conv.LastMessageAtUtc,
                                           member.LastReadLaterMessageId,
                                           LastMessageSenderId = lastMessageInfo != null ? (Guid?)lastMessageInfo.SenderId : null,
                                           LastMessageIsRead = lastMessageInfo != null && lastMessageInfo.IsRead,
                                           LastMessageId = lastMessageInfo != null ? (Guid?)lastMessageInfo.Id : null,
                                           member.IsPinned,
                                           member.IsMuted,
                                           member.IsMarkedReadLater
                                       })
                                       .ToListAsync(cancellationToken);

            // Batch query: Get ALL unread messages, then find first for each conversation in-memory
            var conversationIds = conversationsQuery.Select(c => c.Id).ToList();
            var firstUnreadMessageIds = new Dictionary<Guid, Guid>();
            var unreadCountDictionary = new Dictionary<Guid, int>();
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
                var groupedUnreadMessages = allUnreadMessages.GroupBy(m => m.ConversationId);

                firstUnreadMessageIds = groupedUnreadMessages
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(m => m.CreatedAtUtc).First().Id
                    );

                unreadCountDictionary = groupedUnreadMessages
                    .ToDictionary(
                        g => g.Key,
                        g => g.Count()
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
                    c.OtherUserEmail,
                    c.FullName,
                    c.AvatarUrl,
                    c.LastMessage,
                    c.LastMessageAtUtc,
                    unreadCountDictionary.TryGetValue(c.Id, out int unreadCount) ? unreadCount : 0,
                    hasUnreadMentionsDictionary.ContainsKey(c.Id) && hasUnreadMentionsDictionary[c.Id],
                    c.LastReadLaterMessageId,
                    c.LastMessageSenderId,
                    status,
                    c.LastMessageId,
                    firstUnreadMessageIds.TryGetValue(c.Id, out Guid value) ? (Guid?)value : null,
                    c.IsNotes,
                    c.IsPinned,
                    c.IsMuted,
                    c.IsMarkedReadLater
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