namespace ChatApp.Modules.Identity.Application.DTOs
{
    public record UserDto(
        Guid Id,
        string Username,
        string Email,
        bool IsActive,
        bool IsAdmin,
        DateTime CreatedAtUtc);
}