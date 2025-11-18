using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Admin.Services;

/// <summary>
/// Interface for permission management operations
/// </summary>
public interface IPermissionService
{
    Task<Result<List<PermissionDto>>> GetPermissionsAsync(string? module = null);
    Task<Result> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<Result> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
}
