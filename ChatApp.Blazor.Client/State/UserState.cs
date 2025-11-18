using ChatApp.Blazor.Client.Models.Auth;

namespace ChatApp.Blazor.Client.State;

/// <summary>
/// Current user state management
/// </summary>
public class UserState
{
    private UserDto? _currentUser;

    public event Action? OnChange;

    public UserDto? CurrentUser
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

    public string? Username => _currentUser?.Username;

    public string? DisplayName => _currentUser?.DisplayName;

    public string? AvatarUrl => _currentUser?.AvatarUrl;

    public List<string> Permissions
    {
        get
        {
            if (_currentUser == null)
            {
                return new List<string>();
            }

            return _currentUser.Roles
                .SelectMany(r => r.Permissions)
                .Select(p => p.Name ?? "")
                .Distinct()
                .ToList();
        }
    }

    public bool HasPermission(string permission)
    {
        if (IsAdmin)
        {
            return true;
        }

        return Permissions.Contains(permission);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
