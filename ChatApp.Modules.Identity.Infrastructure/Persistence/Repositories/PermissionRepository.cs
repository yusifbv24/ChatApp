using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Repositories
{
    public class PermissionRepository:IPermissionRepository
    {
        private readonly IdentityDbContext _context;

        public PermissionRepository(IdentityDbContext context)
        {
            _context= context;
        }

        public async Task AddAsync(Permission entity, CancellationToken cancellationToken = default)
        {
            await _context.Permissions.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }


        public async Task DeleteAsync(Permission entity, CancellationToken cancellationToken = default)
        {
            _context.Permissions.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }


        public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Permissions.ToListAsync(cancellationToken);
        }


        public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Permissions
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }


        public async Task<List<Permission>> GetByModuleAsync(
            string module, 
            CancellationToken cancellationToken = default)
        {
            return await _context.Permissions
                .Where(p=>p.Module==module)
                .ToListAsync(cancellationToken);
        }


        public async Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Permissions
                .FirstOrDefaultAsync(p=>p.Name==name, cancellationToken);
        }


        public async Task<List<Permission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .Distinct()
                .ToListAsync(cancellationToken);
        }


        public async Task UpdateAsync(Permission entity, CancellationToken cancellationToken = default)
        {
            _context.Permissions.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}