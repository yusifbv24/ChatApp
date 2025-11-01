using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Repositories
{
    public class Repository<T> : IRepository<T> where T : Entity
    {
        protected readonly IdentityDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(IdentityDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync([id], cancellationToken);
        }


        public async Task<T?> GetByNameAsync(
            string name, 
            CancellationToken cancellationToken = default)
        {
            // Check if entity type has a Name property using reflection
            var nameProperty=typeof(T).GetProperty("Name");
            if(nameProperty == null)
            {
                throw new InvalidOperationException($"Entity type {typeof(T).Name} does not have a Name property");
            }

            // Build the expression dynamically : entity=>entity.Name==name
            var paramter = Expression.Parameter(typeof(T), "entity");
            var property= Expression.Property(paramter, nameProperty);
            var constant= Expression.Constant(name);
            var equality= Expression.Equal(constant, paramter);
            var lambda =  Expression.Lambda<Func<T, bool>>(equality, paramter);

            return await _dbSet.FirstOrDefaultAsync(lambda, cancellationToken);
        }



        public async Task<T?> GetFirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
        }



        /// <summary>
        /// Optimized method to retrieve user permissions with eager loading
        /// This prevents N+1 query problems by loading all related data in a single query
        /// </summary>
        public async Task<List<Permission>> GetPermissionsByUserIdAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .Distinct()
                .ToListAsync(cancellationToken);
        }



        public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }



        public async Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        }



        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
        }



        public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }


        public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }


        public async Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }
    }
}