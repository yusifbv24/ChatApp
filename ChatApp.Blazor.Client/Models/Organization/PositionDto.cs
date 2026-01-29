namespace ChatApp.Blazor.Client.Models.Organization;

public record PositionDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? DepartmentId,
    string? DepartmentName,
    DateTime CreatedAtUtc);