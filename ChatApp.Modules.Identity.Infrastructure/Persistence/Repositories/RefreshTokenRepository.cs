using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository:IRefreshTokenRepository
    {
        private readonly IdentityDbContext _context;
        public RefreshTokenRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RefreshToken entity, CancellationToken cancellationToken = default)
        {
            await _context.RefreshTokens.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken); 
        }


        public async Task DeleteAsync(RefreshToken entity, CancellationToken cancellationToken = default)
        {
            _context.RefreshTokens.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }


        public async Task<IReadOnlyList<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens.ToListAsync(cancellationToken);
        }


        public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens.FirstOrDefaultAsync(x=>x.Token== token, cancellationToken);
        }


        public async Task<List<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Where(r=>r.UserId==userId)
                .ToListAsync();
        }


        public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var tokens=await GetByUserIdAsync(userId, cancellationToken);
            foreach(var token in tokens)
            {
                token.Revoke();
            }
            await _context.SaveChangesAsync(cancellationToken);
        }


        public async Task UpdateAsync(RefreshToken entity, CancellationToken cancellationToken = default)
        {
            _context.RefreshTokens.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}