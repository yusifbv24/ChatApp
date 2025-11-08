namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record UpdateUserRequest(
        string? Email,
        string? DisplayName,
        string? AvatarUrl,
        string? Notes
    );
}