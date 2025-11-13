using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Admin.Services;

/// <summary>
/// Implementation of permission management service
/// Handles all permission-related API endpoints
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IApiClient _apiClient;

    public PermissionService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets all permissions - GET /api/permissions
    /// Requires: Roles.Read permission
    /// </summary>
    public async Task<Result<List<PermissionDto>>> GetPermissionsAsync(string? module = null)
    {
        var endpoint = string.IsNullOrEmpty(module)
            ? "/api/permissions"
            : $"/api/permissions?module={module}";

        return await _apiClient.GetAsync<List<PermissionDto>>(endpoint);
    }

    /// <summary>
    /// Assigns permission to role - POST /api/permissions/roles/{roleId}/permissions/{permissionId}
    /// Requires: Roles.Create permission
    /// </summary>
    public async Task<Result> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        return await _apiClient.PostAsync($"/api/permissions/roles/{roleId}/permissions/{permissionId}");
    }

    /// <summary>
    /// Removes permission from role - DELETE /api/permissions/roles/{roleId}/permissions/{permissionId}
    /// Requires: Roles.Delete permission
    /// </summary>
    public async Task<Result> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        return await _apiClient.DeleteAsync($"/api/permissions/roles/{roleId}/permissions/{permissionId}");
    }
}
