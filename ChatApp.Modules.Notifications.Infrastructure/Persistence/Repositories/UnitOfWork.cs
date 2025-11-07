using ChatApp.Modules.Notifications.Application.Interfaces;
using ChatApp.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChatApp.Modules.Notifications.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly NotificationsDbContext _context;
        private IDbContextTransaction? _transaction;

        public INotificationRepository Notifications { get; }

        public UnitOfWork(NotificationsDbContext context)
        {
            _context = context;
            Notifications = new NotificationRepository(context);
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