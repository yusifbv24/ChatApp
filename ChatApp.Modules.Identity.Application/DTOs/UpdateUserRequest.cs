namespace ChatApp.Modules.Identity.Application.DTOs
{
    public record UpdateUserRequest(
        string? Email,
        string? DisplayName,
        string? AvatarUrl,
        string? Notes
    );
}