namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Data transfer object for user information
/// </summary>
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
    DateTime CreatedAtUtc,
    List<RoleDto> Roles
);
