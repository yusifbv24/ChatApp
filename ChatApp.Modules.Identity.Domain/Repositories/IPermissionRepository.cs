using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Interfaces;

namespace ChatApp.Modules.Identity.Domain.Repositories
{
    public interface IPermissionRepository:IRepository<Permission>
    {
        Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<List<Permission>> GetByModuleAsync(string module, CancellationToken cancellationToken = default);
        Task<List<Permission>> GetByUserIdAsync(Guid userId,CancellationToken cancellationToken = default);
    }
}