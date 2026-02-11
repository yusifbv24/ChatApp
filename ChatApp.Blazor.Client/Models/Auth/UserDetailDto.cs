namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Complete user information for detail views and profile pages
/// Includes all user data, organizational structure, and permissions
/// </summary>
public record UserDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string? Position,
    Guid? PositionId,
    string? AvatarUrl,
    string? AboutMe,
    DateTime? DateOfBirth,
    string? WorkPhone,
    DateTime? HiringDate,
    DateTime? LastVisit,
    bool IsActive,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? SupervisorId,
    string? SupervisorName,
    string? SupervisorAvatarUrl,
    string? SupervisorPosition,
    bool IsHeadOfDepartment,
    List<SubordinateDto> Subordinates,
    List<string> Permissions,
    bool IsSuperAdmin,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public string FullName => $"{FirstName} {LastName}";
    public bool IsAdmin => Role == RoleNames.Administrator;
}

public record SubordinateDto(
    Guid Id,
    string FullName,
    string? Position,
    string? AvatarUrl,
    bool IsActive);