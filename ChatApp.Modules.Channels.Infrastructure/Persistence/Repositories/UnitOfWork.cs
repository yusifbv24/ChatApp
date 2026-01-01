using ChatApp.Modules.Channels.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ChannelsDbContext _context;
        private IDbContextTransaction? _transaction;

        public IChannelRepository Channels { get; }
        public IChannelMemberRepository ChannelMembers { get; }
        public IChannelMessageRepository ChannelMessages { get; }
        public IChannelMessageReadRepository ChannelMessageReads { get; }
        public IChannelMessageReactionRepository ChannelMessageReactions { get; }
        public IUserFavoriteChannelMessageRepository Favorites { get; }

        public UnitOfWork(ChannelsDbContext context)
        {
            _context = context;
            Channels = new ChannelRepository(context);
            ChannelMembers = new ChannelMemberRepository(context);
            ChannelMessages = new ChannelMessageRepository(context);
            ChannelMessageReads = new ChannelMessageReadRepository(context);
            ChannelMessageReactions = new ChannelMessageReactionRepository(context);
            Favorites = new UserFavoriteChannelMessageRepository(context);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}