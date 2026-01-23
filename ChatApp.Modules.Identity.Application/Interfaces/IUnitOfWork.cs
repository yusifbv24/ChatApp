using ChatApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Identity.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        DbSet<User> Users { get; }
        DbSet<Employee> Employees { get; }
        DbSet<Department> Departments { get; }
        DbSet<Position> Positions { get; }
        DbSet<UserPermission> UserPermissions { get; }
        DbSet<RefreshToken> RefreshTokens { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}