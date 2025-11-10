using ChatApp.Client.Constants;
using ChatApp.Client.Models.Common;
using ChatApp.Client.Models.Identity;

namespace ChatApp.Client.Services.Api
{
    public class PermissionService : IPermissionService
    {
        private readonly IApiClient _apiClient;

        public PermissionService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<Result<List<PermissionDto>>> GetPermissionsAsync(string? module = null)
        {
            return await _apiClient.GetAsync<List<PermissionDto>>(
                ApiEndpoints.Permissions.GetPermissions(module));
        }

        public async Task<Result> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
        {
            return await _apiClient.PostAsync(
                ApiEndpoints.Permissions.AssignPermissionToRole(roleId, permissionId),
                new { });
        }

        public async Task<Result> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
        {
            return await _apiClient.DeleteAsync(
                ApiEndpoints.Permissions.RemovePermissionFromRole(roleId, permissionId));
        }
    }
}