namespace ChatApp.Blazor.Client.Models.Auth
{
    public record RoleDto(
        Guid Id,
        string? Name,
        string? Description,
        bool IsSystemRole,
        List<PermissionDto> Permissions,
        int UserCount,
        DateTime CreatedAtUtc);
}