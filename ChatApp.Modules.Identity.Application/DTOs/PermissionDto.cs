namespace ChatApp.Modules.Identity.Application.DTOs
{
    public record PermissionDto(
        Guid Id,
        string Name,
        string Description,
        string Module);
}