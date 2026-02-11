namespace ChatApp.Blazor.Client.State;

/// <summary>
/// Global application state management
/// </summary>
public class AppState
{
    private bool _isDarkMode;
    private List<Guid> _onlineUsers = new();
    private int _unreadNotificationCount;
    private int _unreadMessageCount;
    private Guid? _pendingChatUserId;

    public event Action? OnChange;

    /// <summary>
    /// ProfilePanel-dən chat başlatmaq üçün istifadə olunur.
    /// Messages səhifəsi açıldıqda bir dəfə oxunur və sıfırlanır.
    /// Conversation yaratmadan istifadəçi ilə chat açmağı təmin edir.
    /// </summary>
    public Guid? PendingChatUserId
    {
        get => _pendingChatUserId;
        set => _pendingChatUserId = value;
    }

    /// <summary>
    /// PendingChatUserId-ni oxuyub sıfırlayır (bir dəfəlik istifadə).
    /// </summary>
    public Guid? ConsumePendingChatUserId()
    {
        var userId = _pendingChatUserId;
        _pendingChatUserId = null;
        return userId;
    }

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

    public int UnreadMessageCount
    {
        get => _unreadMessageCount;
        set
        {
            if (_unreadMessageCount != value)
            {
                _unreadMessageCount = value;
                NotifyStateChanged();
            }
        }
    }

    public void IncrementUnreadMessages()
    {
        _unreadMessageCount++;
        NotifyStateChanged();
    }

    public void DecrementUnreadMessages(int count = 1)
    {
        _unreadMessageCount = Math.Max(0, _unreadMessageCount - count);
        NotifyStateChanged();
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