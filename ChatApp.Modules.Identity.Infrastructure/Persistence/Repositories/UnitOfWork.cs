using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IdentityDbContext _context;
        private IDbContextTransaction? _transaction;
        public IRepository<User> Users { get; }
        public IRepository<RefreshToken> RefreshTokens { get; }
        public IRepository<Role> Roles { get; }
        public IRepository<UserRole> UserRoles { get; }
        public IRepository<Permission> Permissions { get; }
        public IRepository<RolePermission> RolePermissions { get; }

        public UnitOfWork(IdentityDbContext context)
        {
            _context = context;
            Users = new Repository<User>(context);
            RefreshTokens = new Repository<RefreshToken>(context);
            Roles = new Repository<Role>(context);
            UserRoles = new Repository<UserRole>(context);
            Permissions = new Repository<Permission>(context);
            RolePermissions= new Repository<RolePermission>(context);
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