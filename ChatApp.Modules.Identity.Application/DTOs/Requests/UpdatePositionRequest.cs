namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record UpdatePositionRequest(
        string? Name = null,
        Guid? DepartmentId = null,
        string? Description = null);
}
