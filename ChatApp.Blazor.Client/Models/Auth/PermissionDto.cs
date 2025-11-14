namespace ChatApp.Blazor.Client.Models.Auth
{
    public record PermissionDto(
        Guid Id,
        string? Name,
        string? Description,
        string? Module);
}