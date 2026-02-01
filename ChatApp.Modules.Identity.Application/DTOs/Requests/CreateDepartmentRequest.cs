namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record CreateDepartmentRequest(
        string Name,
        Guid CompanyId,
        Guid? ParentDepartmentId = null);
}
