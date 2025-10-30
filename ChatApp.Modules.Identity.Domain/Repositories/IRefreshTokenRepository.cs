using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Interfaces;

namespace ChatApp.Modules.Identity.Domain.Repositories
{
    public interface IRefreshTokenRepository:IRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<List<RefreshToken>> GetByUserIdAsync(Guid userId,CancellationToken cancellationToken = default);
        Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}