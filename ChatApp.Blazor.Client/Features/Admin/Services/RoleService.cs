using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Admin.Services
{
    public class RoleService : IRoleService
    {
        private readonly IApiClient _apiClient;

        public RoleService(IApiClient apiClient)
        {
            _apiClient= apiClient;
        }
        public async Task<Result<Guid>> CreateRoleAsync(CreateRoleRequest request)
        {
            return await _apiClient.PostAsync<Guid>("/api/roles", request);
        }

        public async Task<Result> DeleteRoleAsync(Guid roleId)
        {
            return await _apiClient.DeleteAsync($"/api/roles/{roleId}");
        }

        public async Task<Result<List<RoleDto>>> GetRolesAsync()
        {
            return await _apiClient.GetAsync<List<RoleDto>>("/api/roles");
        }

        public async Task<Result> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request)
        {
            return await _apiClient.PutAsync($"/api/roles/{roleId}", request);
        }
    }
}