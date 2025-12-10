using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DirectMessagesDbContext _context;
        private IDbContextTransaction? _transaction;

        public IDirectConversationRepository Conversations { get; }
        public IDirectMessageRepository Messages { get; }
        public IDirectMessageReactionRepository Reactions { get; }

        public UnitOfWork(DirectMessagesDbContext context,IConnectionManager connectionManager)
        {
            _context = context;
            Conversations = new DirectConversationRepository(context,connectionManager);
            Messages = new DirectMessageRepository(context);
            Reactions = new DirectMessageReactionRepository(context);
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