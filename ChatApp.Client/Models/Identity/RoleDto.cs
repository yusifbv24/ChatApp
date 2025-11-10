namespace ChatApp.Client.Models.Identity
{
    public record RoleDto(
        Guid Id,
        string Name,
        string? Description,
        int UserCount,
        int PermissionCount,
        List<Guid> PermissionIds
    );
}