using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Admin.Services;

/// <summary>
/// Implementation of role management service
/// Handles all role-related API endpoints
/// </summary>
public class RoleService : IRoleService
{
    private readonly IApiClient _apiClient;

    public RoleService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets all roles - GET /api/roles
    /// Requires: Roles.Read permission
    /// </summary>
    public async Task<Result<List<RoleDto>>> GetRolesAsync()
    {
        return await _apiClient.GetAsync<List<RoleDto>>("/api/roles");
    }

    /// <summary>
    /// Creates a new role - POST /api/roles
    /// Requires: Roles.Create permission
    /// </summary>
    public async Task<Result<Guid>> CreateRoleAsync(CreateRoleRequest request)
    {
        return await _apiClient.PostAsync<Guid>("/api/roles", request);
    }

    /// <summary>
    /// Updates role information - PUT /api/roles/{roleId}
    /// Requires: Roles.Update permission
    /// </summary>
    public async Task<Result> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request)
    {
        return await _apiClient.PutAsync($"/api/roles/{roleId}", request);
    }

    /// <summary>
    /// Deletes a role - DELETE /api/roles/{roleId}
    /// Requires: Roles.Delete permission
    /// </summary>
    public async Task<Result> DeleteRoleAsync(Guid roleId)
    {
        return await _apiClient.DeleteAsync($"/api/roles/{roleId}");
    }
}
