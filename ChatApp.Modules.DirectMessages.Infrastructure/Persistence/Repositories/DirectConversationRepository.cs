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
            // 2. User is NOT the initiator AND the conversation has messages
            var conversationsQuery = await (from conv in _context.DirectConversations
                                       where ((conv.User1Id == userId && conv.IsUser1Active) ||
                                              (conv.User2Id == userId && conv.IsUser2Active))
                                             && (conv.InitiatedByUserId == userId || conv.HasMessages)
                                       let otherUserId = conv.User1Id == userId ? conv.User2Id : conv.User1Id
                                       join user in _context.Set<UserReadModel>() on otherUserId equals user.Id
                                       let lastMessageInfo = _context.DirectMessages
                                          .Where(m => m.ConversationId == conv.Id)
                                          .OrderByDescending(m => m.CreatedAtUtc)
                                          .Select(m => new { m.Id, m.Content, m.IsDeleted, m.SenderId, m.IsRead })
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
                                           OtherUserId=otherUserId,
                                           user.Username,
                                           user.DisplayName,
                                           user.AvatarUrl,
                                           LastMessage = lastMessageInfo != null
                                               ? (lastMessageInfo.IsDeleted ? "This message was deleted" : lastMessageInfo.Content)
                                               : null,
                                           conv.LastMessageAtUtc,
                                           UnreadCount=unreadCount,
                                           LastReadLaterMessageId=lastReadLaterMessageId,
                                           LastMessageSenderId = lastMessageInfo != null ? (Guid?)lastMessageInfo.SenderId : null,
                                           LastMessageIsRead = lastMessageInfo != null && lastMessageInfo.IsRead,
                                           LastMessageId = lastMessageInfo != null ? (Guid?)lastMessageInfo.Id : null
                                       })
                                       .ToListAsync(cancellationToken);

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
                    c.LastReadLaterMessageId,
                    c.LastMessageSenderId,
                    status,
                    c.LastMessageId
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