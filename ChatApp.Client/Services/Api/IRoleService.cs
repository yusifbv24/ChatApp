using ChatApp.Client.Models.Common;
using ChatApp.Client.Models.Identity;

namespace ChatApp.Client.Services.Api
{
    public interface IRoleService
    {
        Task<Result<List<RoleDto>>> GetRolesAsync();
        Task<Result<RoleDto>> GetRoleByIdAsync(Guid roleId);
        Task<Result<Guid>> CreateRoleAsync(CreateRoleRequest request);
        Task<Result> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request);
        Task<Result> DeleteRoleAsync(Guid roleId);
    }
}