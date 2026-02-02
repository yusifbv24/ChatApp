using ChatApp.Blazor.Client.Models.Auth;

namespace ChatApp.Blazor.Client.State;

/// <summary>
/// Current user state management
/// </summary>
public class UserState
{
    private UserDetailDto? _currentUser;

    public event Action? OnChange;

    public UserDetailDto? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            NotifyStateChanged();
        }
    }

    public bool IsAuthenticated => _currentUser != null;

    public bool IsAdmin => _currentUser?.IsAdmin ?? false;

    public Guid? UserId => _currentUser?.Id;

    public string? FirstName => _currentUser?.FirstName;

    public string? LastName => _currentUser?.LastName;

    public string? FullName => _currentUser?.FullName;

    public string? Email => _currentUser?.Email;

    public string? AvatarUrl => _currentUser?.AvatarUrl;

    public string? Position => _currentUser?.Position;

    public string? DepartmentName => _currentUser?.DepartmentName;

    public List<string> Permissions => _currentUser?.Permissions ?? [];

    public bool HasPermission(string permission)
    {
        // Only Super Admin bypasses all permission checks
        // Regular admins are subject to their assigned permissions
        if (_currentUser?.IsSuperAdmin == true)
        {
            return true;
        }

        return Permissions.Contains(permission);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}