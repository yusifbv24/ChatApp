using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Auth.Services
{
    public class UserService : IUserService
    {
        private readonly IApiClient _apiClient;

        public UserService(IApiClient apiClient)
        {
            _apiClient= apiClient;
        }

        public async Task<Result> ActivateUserAsync(Guid userId)
        {
            return await _apiClient.PostAsync($"/api/users/{userId}/activate");
        }

        public async Task<Result> AssignRoleAsync(Guid userId, Guid roleId)
        {
            return await _apiClient.PostAsync($"/api/users/{userId}/roles/{roleId}");
        }

        public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request)
        {
            return await _apiClient.PostAsync("/api/users/me/change-password", request);
        }

        public async Task<Result> ChangeUserPasswordAsync(Guid userId, AdminChangePasswordRequest request)
        {
            return await _apiClient.PostAsync($"/api/users/{userId}/change-password", request);
        }

        public async Task<Result<Guid>> CreateUserAsync(CreateUserRequest request)
        {
            return await _apiClient.PostAsync<Guid>("/api/users", request);
        }

        public async Task<Result> DeactivateUserAsync(Guid userId)
        {
            return await _apiClient.PostAsync($"/api/users/{userId}/deactivate");
        }

        public async Task<Result> DeleteUserAsync(Guid userId)
        {
            return await _apiClient.DeleteAsync($"/api/users/{userId}");
        }

        public async Task<Result<UserDto>> GetCurrentUserAsync()
        {
            return await _apiClient.GetAsync<UserDto>("/api/users/me");
        }

        public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId)
        {
            return await _apiClient.GetAsync<UserDto>($"/api/users/{userId}");
        }

        public async Task<Result<List<UserDto>>> GetUsersAsync(
            int pageNumber = 1, 
            int pageSize = 10)
        {
            return await _apiClient.GetAsync<List<UserDto>>(
                $"/api/users?pageNumber={pageNumber}&pageSize={pageSize}");
        }

        public async Task<Result> RemoveRoleAsync(Guid userId, Guid roleId)
        {
            return await _apiClient.DeleteAsync($"/api/users/{userId}/roles/{roleId}");
        }

        public async Task<Result> UpdateCurrentUserAsync(UpdateUserRequest request)
        {
            return await _apiClient.PutAsync("/api/users/me", request);
        }

        public async Task<Result> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            return await _apiClient.PutAsync($"/api/users/{userId}", request);
        }
    }
}