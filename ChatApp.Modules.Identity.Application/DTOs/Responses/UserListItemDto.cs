namespace ChatApp.Modules.Identity.Application.DTOs.Responses
{
    /// <summary>
    /// User information for list views (admin panel, user management)
    /// Contains essential information without heavy details like permissions
    /// </summary>
    public record UserListItemDto(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string Role,
        string? Position,
        string? AvatarUrl,
        bool IsActive,
        bool IsCEO,
        string? DepartmentName,
        DateTime CreatedAtUtc)
    {
        public string FullName => $"{FirstName} {LastName}";
    };
}