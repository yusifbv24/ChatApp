using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Repositories
{
    public class DirectMessageReactionRepository : IDirectMessageReactionRepository
    {
        private readonly DirectMessagesDbContext _context;

        public DirectMessageReactionRepository(DirectMessagesDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DirectMessageReaction reaction, CancellationToken cancellationToken = default)
        {
            await _context.DirectMessageReactions.AddAsync(reaction, cancellationToken);
        }

        public Task DeleteAsync(DirectMessageReaction reaction, CancellationToken cancellationToken = default)
        {
            _context.DirectMessageReactions.Remove(reaction);
            return Task.CompletedTask;
        }
    }
}
