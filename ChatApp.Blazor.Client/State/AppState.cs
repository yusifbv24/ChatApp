namespace ChatApp.Blazor.Client.State;

/// <summary>
/// Global application state management
/// </summary>
public class AppState
{
    private bool _isDarkMode;
    private List<Guid> _onlineUsers = new();
    private int _unreadNotificationCount;

    public event Action? OnChange;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                NotifyStateChanged();
            }
        }
    }

    public List<Guid> OnlineUsers
    {
        get => _onlineUsers;
        set
        {
            _onlineUsers = value;
            NotifyStateChanged();
        }
    }

    public int UnreadNotificationCount
    {
        get => _unreadNotificationCount;
        set
        {
            if (_unreadNotificationCount != value)
            {
                _unreadNotificationCount = value;
                NotifyStateChanged();
            }
        }
    }

    public void AddOnlineUser(Guid userId)
    {
        if (!_onlineUsers.Contains(userId))
        {
            _onlineUsers.Add(userId);
            NotifyStateChanged();
        }
    }

    public void RemoveOnlineUser(Guid userId)
    {
        if (_onlineUsers.Remove(userId))
        {
            NotifyStateChanged();
        }
    }

    public bool IsUserOnline(Guid userId)
    {
        return _onlineUsers.Contains(userId);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
