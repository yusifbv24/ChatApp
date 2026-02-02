using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Auth.Services;

/// <summary>
/// Interface for user management operations
/// </summary>
public interface IUserService
{
    // Current user operations
    Task<Result<UserDetailDto>> GetCurrentUserAsync();
    Task<Result> UpdateCurrentUserAsync(UpdateUserRequest request);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request);

    // User management operations (admin)
    Task<Result<UserDetailDto>> GetUserByIdAsync(Guid userId);
    Task<Result<List<UserListItemDto>>> GetUsersAsync(int pageNumber = 1, int pageSize = 10);
    Task<Result<CreateUserResponse>> CreateUserAsync(CreateUserRequest request);
    Task<Result> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<Result> ActivateUserAsync(Guid userId);
    Task<Result> DeactivateUserAsync(Guid userId);
    Task<Result> DeleteUserAsync(Guid userId);
    Task<Result> ChangeUserPasswordAsync(AdminChangePasswordRequest request);

    // Permission management
    Task<Result> AssignPermissionAsync(Guid userId, string permissionName);
    Task<Result> RemovePermissionAsync(Guid userId, string permissionName);

    // Employee/organizational management
    Task<Result> AssignToDepartmentAsync(Guid userId, Guid departmentId);
    Task<Result> RemoveFromDepartmentAsync(Guid userId);
    Task<Result> AssignSupervisorAsync(Guid userId, Guid supervisorId);
    Task<Result> RemoveSupervisorAsync(Guid userId);

    // Search operations
    Task<Result<List<UserSearchResultDto>>> SearchUsersAsync(string searchTerm);

    // Department users for conversation sidebar
    Task<Result<PagedResult<DepartmentUserDto>>> GetDepartmentUsersAsync(int pageNumber = 1, int pageSize = 20, string? search = null);
}
