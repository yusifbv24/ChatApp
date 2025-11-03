using ChatApp.Modules.DirectMessages.Application.DTOs.Request;
using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Repositories
{
    public class DirectConversationRepository : IDirectConversationRepository
    {
        private readonly DirectMessagesDbContext _context;
        public DirectConversationRepository(DirectMessagesDbContext context)
        {
            _context = context;
        }

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
            var conversations = await (from conv in _context.DirectConversations
                                       where (conv.User1Id == userId && conv.IsUser1Active) ||
                                              (conv.User2Id == userId && conv.IsUser2Active)
                                       let otherUserId = conv.User1Id == userId ? conv.User2Id : conv.User1Id
                                       join user in _context.Set<UserReadModel>() on otherUserId equals user.Id
                                       let lastMessage = _context.DirectMessages
                                          .Where(m => m.ConversationId == conv.Id && !m.IsDeleted)
                                          .OrderByDescending(m => m.CreatedAtUtc)
                                          .Select(m => m.Content)
                                          .FirstOrDefault()
                                       let unreadCount = _context.DirectMessages
                                          .Count(m => m.ConversationId == conv.Id &&
                                                    m.ReceiverId == userId &&
                                                    !m.IsRead &&
                                                    !m.IsDeleted)
                                       orderby conv.LastMessageAtUtc descending
                                       select new DirectConversationDto(
                                           conv.Id,
                                           otherUserId,
                                           user.Username,
                                           user.DisplayName,
                                           user.AvatarUrl,
                                           lastMessage,
                                           conv.LastMessageAtUtc,
                                           unreadCount,
                                           user.IsOnline
                                       ))
                                       .ToListAsync();
            return conversations;
        }



        public Task UpdateAsync(DirectConversation conversation, CancellationToken cancellationToken = default)
        {
            _context.DirectConversations.Update(conversation);
            return Task.CompletedTask;
        }
    }
}