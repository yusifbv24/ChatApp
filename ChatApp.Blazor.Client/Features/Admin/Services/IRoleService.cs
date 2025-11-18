using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Admin.Services;

/// <summary>
/// Interface for role management operations
/// </summary>
public interface IRoleService
{
    Task<Result<List<RoleDto>>> GetRolesAsync();
    Task<Result<Guid>> CreateRoleAsync(CreateRoleRequest request);
    Task<Result> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request);
    Task<Result> DeleteRoleAsync(Guid roleId);
}
