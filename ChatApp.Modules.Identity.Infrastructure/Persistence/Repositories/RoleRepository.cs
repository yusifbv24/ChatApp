using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Repositories
{
    public class RoleRepository:IRoleRepository
    {
        private readonly IdentityDbContext _context;

        public RoleRepository(IdentityDbContext context)
        {
            _context=context;
        }

        public async Task AddAsync(Role entity, CancellationToken cancellationToken = default)
        {
            await _context.Roles.AddAsync(entity,cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }


        public async Task DeleteAsync(Role entity, CancellationToken cancellationToken = default)
        {
            _context.Roles.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }


        public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .ToListAsync(cancellationToken);
        }


        public async Task<Role?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Roles
                .Include(r=>r.RolePermissions)
                .FirstOrDefaultAsync(r=>r.Id == id,cancellationToken);
        }


        public async Task<Role?> GetByNameAsync(
            string name, 
            CancellationToken cancellationToken = default)
        {
            return await _context.Roles
                .Include(r=>r.RolePermissions)
                .FirstOrDefaultAsync(r=>r.Name== name,cancellationToken)    ;
        }


        public async Task<List<Role>> GetRolesWithPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Roles
                .Include(r=>r.RolePermissions)
                .ThenInclude(rp=>rp.Permission)
                .ToListAsync(cancellationToken);
        }


        public async Task UpdateAsync(Role entity, CancellationToken cancellationToken = default)
        {
            _context.Roles.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}