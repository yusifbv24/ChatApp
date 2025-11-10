using ChatApp.Client.Constants;
using ChatApp.Client.Models.Common;
using ChatApp.Client.Models.Identity;

namespace ChatApp.Client.Services.Api
{
    public class RoleService : IRoleService
    {
        private readonly IApiClient _apiClient;

        public RoleService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<Result<List<RoleDto>>> GetRolesAsync()
        {
            return await _apiClient.GetAsync<List<RoleDto>>(ApiEndpoints.Roles.GetRoles);
        }

        public async Task<Result<RoleDto>> GetRoleByIdAsync(Guid roleId)
        {
            return await _apiClient.GetAsync<RoleDto>(ApiEndpoints.Roles.GetRoleById(roleId));
        }

        public async Task<Result<Guid>> CreateRoleAsync(CreateRoleRequest request)
        {
            return await _apiClient.PostAsync<CreateRoleRequest, Guid>(
                ApiEndpoints.Roles.CreateRole, request);
        }

        public async Task<Result> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request)
        {
            return await _apiClient.PutAsync(ApiEndpoints.Roles.UpdateRole(roleId), request);
        }

        public async Task<Result> DeleteRoleAsync(Guid roleId)
        {
            return await _apiClient.DeleteAsync(ApiEndpoints.Roles.DeleteRole(roleId));
        }
    }
}