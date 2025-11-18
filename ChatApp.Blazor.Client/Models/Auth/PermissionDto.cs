namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Data transfer object for permission information
/// </summary>
public record PermissionDto(
    Guid Id,
    string? Name,
    string? Description,
    string? Module
);
