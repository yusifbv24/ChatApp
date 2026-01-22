using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork(IdentityDbContext context) : IUnitOfWork
    {
        private IDbContextTransaction? _transaction;

        public DbSet<User> Users => context.Users;
        public DbSet<Department> Departments => context.Departments;
        public DbSet<Position> Positions => context.Positions;
        public DbSet<UserPermission> UserPermissions => context.UserPermissions;
        public DbSet<RefreshToken> RefreshTokens => context.RefreshTokens;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            await context.SaveChangesAsync(cancellationToken);

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            _transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction is null) return;

            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction is null) return;

            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
