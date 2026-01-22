using ChatApp.Modules.Identity.Domain.Enums;

namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record UpdateUserRequest(
        string? FirstName = null,
        string? LastName = null,
        string? Email = null,
        Role? Role = null,
        Guid? PositionId = null,
        string? AvatarUrl = null,
        string? AboutMe = null,
        DateTime? DateOfBirth = null,
        string? WorkPhone = null,
        DateTime? HiringDate = null
    );
}