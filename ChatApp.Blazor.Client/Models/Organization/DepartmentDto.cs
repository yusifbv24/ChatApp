namespace ChatApp.Blazor.Client.Models.Organization;

public record DepartmentDto(
    Guid Id,
    string Name,
    Guid? ParentDepartmentId,
    string? ParentDepartmentName,
    Guid? HeadOfDepartmentId,
    string? HeadOfDepartmentName,
    DateTime CreatedAtUtc);