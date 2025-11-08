namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record CreateUserRequest(
        string Username,
        string Email,
        string Password,
        string DisplayName,
        Guid CreatedBy,
        bool IsAdmin,
        string? AvatarUrl,
        string? Notes
    );
}