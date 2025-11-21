using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Auth.Services;

/// <summary>
/// Interface for user management operations
/// </summary>
public interface IUserService
{
    Task<Result<UserDto>> GetCurrentUserAsync();
    Task<Result> UpdateCurrentUserAsync(UpdateUserRequest request);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request);
    Task<Result<UserDto>> GetUserByIdAsync(Guid userId);
    Task<Result<List<UserDto>>> GetUsersAsync(int pageNumber = 1, int pageSize = 10);
    Task<Result<Guid>> CreateUserAsync(CreateUserRequest request);
    Task<Result> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<Result> ActivateUserAsync(Guid userId);
    Task<Result> DeactivateUserAsync(Guid userId);
    Task<Result> DeleteUserAsync(Guid userId);
    Task<Result> ChangeUserPasswordAsync(AdminChangePasswordRequest request);
    Task<Result> AssignRoleAsync(Guid userId, Guid roleId);
    Task<Result> RemoveRoleAsync(Guid userId, Guid roleId);
    Task<Result> GrantUserPermissionAsync(Guid userId, Guid permissionId);
    Task<Result> RevokeUserPermissionAsync(Guid userId, Guid permissionId);
}