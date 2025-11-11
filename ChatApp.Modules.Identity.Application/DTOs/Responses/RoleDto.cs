namespace ChatApp.Modules.Identity.Application.DTOs.Responses
{
    public record RoleDto(
        Guid Id,
        string? Name,
        string? Description,
        bool IsSystemRole,
        List<PermissionDto> Permissions,
        int UserCount,
        DateTime CreatedAtUtc
    );
}