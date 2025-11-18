namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Data transfer object for role information
/// </summary>
public record RoleDto(
    Guid Id,
    string? Name,
    string? Description,
    bool IsSystemRole,
    List<PermissionDto> Permissions,
    int UserCount,
    DateTime CreatedAtUtc
);
