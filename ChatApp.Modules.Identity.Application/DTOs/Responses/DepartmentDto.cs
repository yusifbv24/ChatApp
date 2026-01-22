namespace ChatApp.Modules.Identity.Application.DTOs.Responses
{
    public record DepartmentDto(
        Guid Id,
        string Name,
        Guid? ParentDepartmentId,
        string? ParentDepartmentName,
        Guid? HeadOfDepartmentId,
        string? HeadOfDepartmentName,
        DateTime CreatedAtUtc);
}
