using ChatApp.Client.Constants;
using ChatApp.Client.Models.Common;
using ChatApp.Client.Models.Identity;

namespace ChatApp.Client.Services.Api
{
    public class UserService : IUserService
    {
        private readonly IApiClient _apiClient;

        public UserService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<Result<UserDto>> GetCurrentUserAsync()
        {
            return await _apiClient.GetAsync<UserDto>(ApiEndpoints.Users.GetCurrentUser);
        }

        public async Task<Result> UpdateCurrentUserAsync(UpdateUserRequest request)
        {
            return await _apiClient.PutAsync(ApiEndpoints.Users.UpdateCurrentUser, request);
        }

        public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request)
        {
            return await _apiClient.PostAsync(ApiEndpoints.Users.ChangeCurrentUserPassword, request);
        }

        public async Task<Result<List<UserDto>>> GetUsersAsync(int pageNumber = 1, int pageSize = 20)
        {
            return await _apiClient.GetAsync<List<UserDto>>(
                ApiEndpoints.Users.GetUsers(pageNumber, pageSize));
        }

        public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId)
        {
            return await _apiClient.GetAsync<UserDto>(ApiEndpoints.Users.GetUserById(userId));
        }

        public async Task<Result<Guid>> CreateUserAsync(CreateUserRequest request)
        {
            return await _apiClient.PostAsync<CreateUserRequest, Guid>(
                ApiEndpoints.Users.CreateUser, request);
        }

        public async Task<Result> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            return await _apiClient.PutAsync(ApiEndpoints.Users.UpdateUser(userId), request);
        }

        public async Task<Result> DeleteUserAsync(Guid userId)
        {
            return await _apiClient.DeleteAsync(ApiEndpoints.Users.DeleteUser(userId));
        }

        public async Task<Result> AssignRoleAsync(Guid userId, Guid roleId)
        {
            return await _apiClient.PostAsync(
                ApiEndpoints.Users.AssignRole(userId, roleId),
                new { });
        }

        public async Task<Result> RemoveRoleAsync(Guid userId, Guid roleId)
        {
            return await _apiClient.DeleteAsync(ApiEndpoints.Users.RemoveRole(userId, roleId));
        }

        public async Task<Result> AdminChangePasswordAsync(Guid userId, AdminChangePasswordRequest request)
        {
            return await _apiClient.PostAsync(
                ApiEndpoints.Users.AdminChangeUserPassword(userId),
                request);
        }
    }
}