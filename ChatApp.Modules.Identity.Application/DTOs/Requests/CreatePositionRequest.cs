namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record CreatePositionRequest(
        string Name,
        Guid? DepartmentId = null,
        string? Description = null);
}
