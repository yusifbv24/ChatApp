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
    public async Task<Result<UserDetailDto>> GetCurrentUserAsync()
    {
        return await _apiClient.GetAsync<UserDetailDto>("/api/users/me");
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
        return await _apiClient.PutAsync("/api/users/me/change-password", request);
    }


    /// <summary>
    /// Gets user by ID - GET /api/users/{userId}
    /// Requires: Users.Read permission
    /// </summary>
    public async Task<Result<UserDetailDto>> GetUserByIdAsync(Guid userId)
    {
        return await _apiClient.GetAsync<UserDetailDto>($"/api/users/{userId}");
    }


    /// <summary>
    /// Gets paginated list of users - GET /api/users
    /// Requires: Users.Read permission
    /// </summary>
    public async Task<Result<List<UserListItemDto>>> GetUsersAsync(int pageNumber = 1, int pageSize = 10)
    {
        return await _apiClient.GetAsync<List<UserListItemDto>>($"/api/users?pageNumber={pageNumber}&pageSize={pageSize}");
    }


    /// <summary>
    /// Creates a new user - POST /api/users
    /// Requires: Users.Create permission
    /// </summary>
    public async Task<Result<CreateUserResponse>> CreateUserAsync(CreateUserRequest request)
    {
        return await _apiClient.PostAsync<CreateUserResponse>("/api/users", request);
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
    public async Task<Result> ChangeUserPasswordAsync(AdminChangePasswordRequest request)
    {
        return await _apiClient.PutAsync($"/api/users/change-user-password", request);
    }


    /// <summary>
    /// Assigns a permission to user - POST /api/users/{userId}/permissions/{permissionName}
    /// Requires: Permissions.Assign permission
    /// </summary>
    public async Task<Result> AssignPermissionAsync(Guid userId, string permissionName)
    {
        return await _apiClient.PostAsync($"/api/users/{userId}/permissions", new { PermissionName = permissionName });
    }


    /// <summary>
    /// Removes a permission from user - DELETE /api/users/{userId}/permissions/{permissionName}
    /// Requires: Permissions.Revoke permission
    /// </summary>
    public async Task<Result> RemovePermissionAsync(Guid userId, string permissionName)
    {
        return await _apiClient.DeleteAsync($"/api/users/{userId}/permissions/{Uri.EscapeDataString(permissionName)}");
    }


    /// <summary>
    /// Assigns employee to department - POST /api/users/{userId}/department
    /// Requires: Users.Update permission
    /// </summary>
    public async Task<Result> AssignToDepartmentAsync(Guid userId, Guid departmentId)
    {
        return await _apiClient.PostAsync($"/api/users/{userId}/department", new { DepartmentId = departmentId });
    }


    /// <summary>
    /// Removes employee from department - DELETE /api/users/{userId}/department
    /// Requires: Users.Update permission
    /// </summary>
    public async Task<Result> RemoveFromDepartmentAsync(Guid userId)
    {
        return await _apiClient.DeleteAsync($"/api/users/{userId}/department");
    }


    /// <summary>
    /// Assigns supervisor to employee - POST /api/users/{userId}/supervisor
    /// Requires: Users.Update permission
    /// </summary>
    public async Task<Result> AssignSupervisorAsync(Guid userId, Guid supervisorId)
    {
        return await _apiClient.PostAsync($"/api/users/{userId}/supervisor", new { SupervisorId = supervisorId });
    }


    /// <summary>
    /// Removes supervisor from employee - DELETE /api/users/{userId}/supervisor
    /// Requires: Users.Update permission
    /// </summary>
    public async Task<Result> RemoveSupervisorAsync(Guid userId)
    {
        return await _apiClient.DeleteAsync($"/api/users/{userId}/supervisor");
    }


    /// <summary>
    /// Searches users by first name, last name, or email - GET /api/users/search
    /// Any authenticated user can search for other users
    /// </summary>
    public async Task<Result<List<UserSearchResultDto>>> SearchUsersAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            return Result<List<UserSearchResultDto>>.Success(new List<UserSearchResultDto>());
        }
        return await _apiClient.GetAsync<List<UserSearchResultDto>>($"/api/users/search?q={Uri.EscapeDataString(searchTerm)}");
    }
}
