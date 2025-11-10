using ChatApp.Client.Models.Common;
using ChatApp.Client.Models.Identity;

namespace ChatApp.Client.Services.Api
{
    public interface IUserService
    {
        // Current user operations (no special permissions needed)
        Task<Result<UserDto>> GetCurrentUserAsync();
        Task<Result> UpdateCurrentUserAsync(UpdateUserRequest request);
        Task<Result> ChangePasswordAsync(ChangePasswordRequest request);

        // Admin user management operations (requires permissions)
        Task<Result<List<UserDto>>> GetUsersAsync(int pageNumber = 1, int pageSize = 20);
        Task<Result<UserDto>> GetUserByIdAsync(Guid userId);
        Task<Result> AdminChangePasswordAsync(Guid userId, AdminChangePasswordRequest request);
        Task<Result<Guid>> CreateUserAsync(CreateUserRequest request);
        Task<Result> UpdateUserAsync(Guid userId, UpdateUserRequest request);
        Task<Result> DeleteUserAsync(Guid userId);

        // Role assignment
        Task<Result> AssignRoleAsync(Guid userId, Guid roleId);
        Task<Result> RemoveRoleAsync(Guid userId, Guid roleId);

    }
}