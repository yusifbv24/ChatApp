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
        public DbSet<Role> Roles => context.Roles;
        public DbSet<UserRole> UserRoles => context.UserRoles;
        public DbSet<Permission> Permissions => context.Permissions;
        public DbSet<RolePermission> RolePermissions => context.RolePermissions;
        public DbSet<RefreshToken> RefreshTokens => context.RefreshTokens;

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