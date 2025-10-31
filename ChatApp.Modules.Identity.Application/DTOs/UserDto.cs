namespace ChatApp.Modules.Identity.Application.DTOs
{
    public record UserDto(
        Guid Id,
        string Username,
        string Email,
        string DisplayName,
        string? AvatarUrl,
        string? Notes,
        Guid CreatedBy,
        bool IsActive,
        bool IsAdmin,
        DateTime CreatedAtUtc);
}