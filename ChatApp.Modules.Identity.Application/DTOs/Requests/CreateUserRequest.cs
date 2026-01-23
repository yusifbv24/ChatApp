using ChatApp.Modules.Identity.Domain.Enums;

namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record CreateUserRequest(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        Guid DepartmentId,
        Role Role = Role.User,
        Guid? PositionId = null,
        string? AvatarUrl = null,
        string? AboutMe = null,
        DateTime? DateOfBirth = null,
        string? WorkPhone = null,
        DateTime? HiringDate = null
    );
}