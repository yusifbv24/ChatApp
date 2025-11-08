namespace ChatApp.Modules.Identity.Application.DTOs.Responses
{
    public record PermissionDto(
        Guid Id,
        string? Name,
        string? Description,
        string? Module);
}