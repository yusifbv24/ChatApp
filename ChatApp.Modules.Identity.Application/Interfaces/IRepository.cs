using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using System.Linq.Expressions;

namespace ChatApp.Modules.Identity.Application.Interfaces
{
    public interface IRepository<T> where T : Entity
    {
        Task<T?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);


        Task<T?> GetByNameAsync(
            string name,
            CancellationToken cancellationToken = default);


        Task<List<Permission>> GetPermissionsByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);


        Task<T?> GetFirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken);


        Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellation = default);

        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    }
}