using ChatApp.Client.Models.Common;
using ChatApp.Client.Models.Identity;

namespace ChatApp.Client.Services.Api
{
    public interface IPermissionService
    {
        Task<Result<List<PermissionDto>>> GetPermissionsAsync(string? module = null);
        Task<Result> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
        Task<Result> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    }
}