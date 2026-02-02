namespace ChatApp.Modules.Identity.Application.DTOs.Responses;

/// <summary>
/// Lightweight DTO for department colleague listing in conversation sidebar.
/// </summary>
public record DepartmentUserDto(
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl,
    string? PositionName,
    Guid? DepartmentId,
    string? DepartmentName
);