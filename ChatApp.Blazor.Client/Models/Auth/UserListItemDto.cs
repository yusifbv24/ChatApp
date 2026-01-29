namespace ChatApp.Blazor.Client.Models.Auth;

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
    string? DepartmentName,
    DateTime CreatedAtUtc)
{
    public string FullName => $"{FirstName} {LastName}";
    public bool IsAdmin => Role == RoleNames.Administrator;
}
