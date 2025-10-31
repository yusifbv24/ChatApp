using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Repositories
{
    public class UserRepository:IUnitOfWork
    {
        private readonly IdentityDbContext _context;

        public UserRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }


        public async Task DeleteAsync(User entity, CancellationToken cancellationToken = default)
        {
            _context.Users.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }


        public async Task<bool> EmailExistsAsync(
            string email, 
            CancellationToken cancellationToken = default)
        {
            return await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
        }



        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ToListAsync(cancellationToken);
        }



        public async Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == email,cancellationToken);
        }



        public async Task<User?> GetByIdAsync(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u=>u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);
        }



        public async Task<User?> GetByUsernameAsync(
            string username, 
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u=>u.UserRoles)
                .FirstOrDefaultAsync(u=>u.Username == username,cancellationToken);
        }



        public async Task<List<User>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(r=>r.Role)
                .ToListAsync(cancellationToken);
        }


        public async Task<UserRole?> GetUserWithRoleAsync(Guid userId,Guid roleId,CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .FirstOrDefaultAsync(
                r=>r.UserId==userId && 
                r.RoleId==roleId,
                cancellationToken);
        }


        public async Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }


        public async Task<bool> UsernameExistsAsync(
            string email, 
            CancellationToken cancellationToken = default)
        {
            return await _context.Users.AnyAsync(u=>u.Email == email,cancellationToken);
        }



        public async Task<bool> DisplayNameExistsAsync(
            string displayName,
            CancellationToken cancellationToken= default)
        {
            return await _context.Users.AnyAsync(x=>x.DisplayName == displayName,cancellationToken);
        }
    }
}