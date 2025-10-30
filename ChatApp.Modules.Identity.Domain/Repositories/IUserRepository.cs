using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Interfaces;

namespace ChatApp.Modules.Identity.Domain.Repositories
{
    public interface IUserRepository:IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> UsernameExistsAsync(string email,CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email,CancellationToken cancellationToken = default);
        Task<List<User>> GetUsersWithRolesAsync(string username, CancellationToken cancellationToken = default);
    }
}