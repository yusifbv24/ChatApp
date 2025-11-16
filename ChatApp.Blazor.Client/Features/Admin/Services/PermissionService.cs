using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Admin.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IApiClient _apiClient;

        public PermissionService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<Result> AssignPermissionsToRoleAsync(Guid roleId, Guid permissionId)
        {
            return await _apiClient.PostAsync($"/api/permissions/roles/{roleId}/permissions/{permissionId}");
        }

        public async Task<Result<List<PermissionDto>>> GetPermissionsAsync(string? module = null)
        {
            var endpoint=string.IsNullOrEmpty(module)
                ? "/api/permissions"
                : $"/api/permissions?module={module}";

            return await _apiClient.GetAsync<List<PermissionDto>>(endpoint);
        }

        public async Task<Result> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
        {
            return await _apiClient.DeleteAsync($"/api/permissions/roles/{roleId}/permissions/{permissionId}");
        }
    }
}