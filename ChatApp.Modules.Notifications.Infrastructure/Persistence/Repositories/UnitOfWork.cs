using ChatApp.Modules.Notifications.Application.Interfaces;
using ChatApp.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChatApp.Modules.Notifications.Infrastructure.Repositories
{
    public class UnitOfWork(NotificationsDbContext context) : IUnitOfWork
    {
        private IDbContextTransaction? _transaction;

        public INotificationRepository Notifications { get; } = new NotificationRepository(context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
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
            context.Dispose();
        }
    }
}