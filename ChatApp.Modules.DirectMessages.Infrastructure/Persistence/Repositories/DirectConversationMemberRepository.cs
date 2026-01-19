using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Repositories
{
    public class DirectConversationMemberRepository(DirectMessagesDbContext context) : IDirectConversationMemberRepository
    {
        private readonly DirectMessagesDbContext _context = context;

        public async Task<DirectConversationMember?> GetByConversationAndUserAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.DirectConversationMembers
                .FirstOrDefaultAsync(
                    m => m.ConversationId == conversationId && m.UserId == userId,
                    cancellationToken);
        }

        public async Task AddAsync(DirectConversationMember member, CancellationToken cancellationToken = default)
        {
            await _context.DirectConversationMembers.AddAsync(member, cancellationToken);
        }

        public Task UpdateAsync(DirectConversationMember member, CancellationToken cancellationToken = default)
        {
            _context.DirectConversationMembers.Update(member);
            return Task.CompletedTask;
        }
    }
}