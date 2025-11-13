using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Auth.Services;

/// <summary>
/// Implementation of user management service
/// Handles all user-related API endpoints
/// </summary>
public class UserService : IUserService
{
    private readonly IApiClient _apiClient;

    public UserService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets current user profile - GET /api/users/me
    /// </summary>
    public async Task<Result<UserDto>> GetCurrentUserAsync()
    {
        return await _apiClient.GetAsync<UserDto>("/api/users/me");
    }

    /// <summary>
    /// Updates current user profile - PUT /api/users/me
    /// </summary>
    public async Task<Result> UpdateCurrentUserAsync(UpdateUserRequest request)
    {
        return await _apiClient.PutAsync("/api/users/me", request);
    }

    /// <summary>
    /// Changes current user password - POST /api/users/me/change-password
    /// </summary>
    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request)
    {
        return await _apiClient.PostAsync("/api/users/me/change-password", request);
    }

    /// <summary>
    /// Gets user by ID - GET /api/users/{userId}
    /// Requires: Users.Read permission
    /// </summary>
    public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId)
    {
        return await _apiClient.GetAsync<UserDto>($"/api/users/{userId}");
    }

    /// <summary>
    /// Gets paginated list of users - GET /api/users
    /// Requires: Users.Read permission
    /// </summary>
    public async Task<Result<List<UserDto>>> GetUsersAsync(int pageNumber = 1, int pageSize = 10)
    {
        return await _apiClient.GetAsync<List<UserDto>>($"/api/users?pageNumber={pageNumber}&pageSize={pageSize}");
    }

    /// <summary>
    /// Creates a new user - POST /api/users
    /// Requires: Users.Create permission
    /// </summary>
    public async Task<Result<Guid>> CreateUserAsync(CreateUserRequest request)
    {
        return await _apiClient.PostAsync<Guid>("/api/users", request);
    }

    /// <summary>
    /// Updates user information - PUT /api/users/{userId}
    /// Requires: Users.Update permission
    /// </summary>
    public async Task<Result> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        return await _apiClient.PutAsync($"/api/users/{userId}", request);
    }

    /// <summary>
    /// Activates a user - PUT /api/users/{userId}/activate
    /// Requires: Users.Update permission
    /// </summary>
    public async Task<Result> ActivateUserAsync(Guid userId)
    {
        return await _apiClient.PutAsync($"/api/users/{userId}/activate");
    }

    /// <summary>
    /// Deactivates a user - PUT /api/users/{userId}/deactivate
    /// Requires: Users.Update permission
    /// </summary>
    public async Task<Result> DeactivateUserAsync(Guid userId)
    {
        return await _apiClient.PutAsync($"/api/users/{userId}/deactivate");
    }

    /// <summary>
    /// Deletes a user - DELETE /api/users/{userId}
    /// Requires: Users.Delete permission
    /// </summary>
    public async Task<Result> DeleteUserAsync(Guid userId)
    {
        return await _apiClient.DeleteAsync($"/api/users/{userId}");
    }

    /// <summary>
    /// Changes user password (admin) - POST /api/users/change-password/{userId}
    /// Requires: Users.Update permission
    /// </summary>
    public async Task<Result> ChangeUserPasswordAsync(Guid userId, AdminChangePasswordRequest request)
    {
        return await _apiClient.PostAsync($"/api/users/change-password/{userId}", request);
    }

    /// <summary>
    /// Assigns a role to user - POST /api/users/{userId}/roles/{roleId}
    /// Requires: Users.Update permission
    /// </summary>
    public async Task<Result> AssignRoleAsync(Guid userId, Guid roleId)
    {
        return await _apiClient.PostAsync($"/api/users/{userId}/roles/{roleId}");
    }

    /// <summary>
    /// Removes a role from user - DELETE /api/users/{userId}/roles/{roleId}
    /// Requires: Users.Update permission
    /// </summary>
    public async Task<Result> RemoveRoleAsync(Guid userId, Guid roleId)
    {
        return await _apiClient.DeleteAsync($"/api/users/{userId}/roles/{roleId}");
    }
}
