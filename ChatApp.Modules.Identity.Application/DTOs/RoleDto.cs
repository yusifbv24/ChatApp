namespace ChatApp.Modules.Identity.Application.DTOs
{
    public record RoleDto(
        Guid Id,
        string? Name,
        string? Description,
        bool IsSystemRole);
}