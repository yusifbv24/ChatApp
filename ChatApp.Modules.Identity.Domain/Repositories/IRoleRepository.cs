using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Interfaces;

namespace ChatApp.Modules.Identity.Domain.Repositories
{
    public interface IRoleRepository:IRepository<Role>
    {
        Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<List<Role>> GetRolesWithPermissionsAsync(CancellationToken cancellationToken = default);
    }
}