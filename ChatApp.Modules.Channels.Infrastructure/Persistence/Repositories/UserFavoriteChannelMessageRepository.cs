using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Repositories
{
    public class UserFavoriteChannelMessageRepository : IUserFavoriteChannelMessageRepository
    {
        private readonly ChannelsDbContext _context;

        public UserFavoriteChannelMessageRepository(ChannelsDbContext context)
        {
            _context = context;
        }

        public async Task<UserFavoriteChannelMessage?> GetAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFavoriteChannelMessages
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MessageId == messageId, cancellationToken);
        }

        public async Task<List<Guid>> GetFavoriteMessageIdsAsync(Guid userId, Guid channelId, CancellationToken cancellationToken = default)
        {
            return await (
                from favorite in _context.UserFavoriteChannelMessages
                join message in _context.ChannelMessages on favorite.MessageId equals message.Id
                where favorite.UserId == userId && message.ChannelId == channelId
                select favorite.MessageId
            ).ToListAsync(cancellationToken);
        }

        public async Task<bool> IsFavoriteAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFavoriteChannelMessages
                .AnyAsync(f => f.UserId == userId && f.MessageId == messageId, cancellationToken);
        }

        public async Task AddAsync(UserFavoriteChannelMessage favorite, CancellationToken cancellationToken = default)
        {
            await _context.UserFavoriteChannelMessages.AddAsync(favorite, cancellationToken);
        }

        public Task RemoveAsync(UserFavoriteChannelMessage favorite, CancellationToken cancellationToken = default)
        {
            _context.UserFavoriteChannelMessages.Remove(favorite);
            return Task.CompletedTask;
        }
    }
}
