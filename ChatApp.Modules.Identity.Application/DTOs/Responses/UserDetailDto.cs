namespace ChatApp.Modules.Identity.Application.DTOs.Responses
{
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
        string? AvatarUrl,
        string? AboutMe,
        DateTime? DateOfBirth,
        string? WorkPhone,
        DateTime? HiringDate,
        DateTime? LastVisit,
        bool IsActive,
        bool IsCEO,
        Guid? DepartmentId,
        string? DepartmentName,
        Guid? SupervisorId,
        string? SupervisorName,
        List<string> Permissions,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc)
    {
        public string FullName => $"{FirstName} {LastName}";
    };
}