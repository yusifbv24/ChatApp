using ChatApp.Blazor.Client.Models.Channels;

namespace ChatApp.Blazor.Client.State;

/// <summary>
/// Channel state management
/// </summary>
public class ChannelState
{
    private List<ChannelDto> _myChannels = new();
    private ChannelDetailsDto? _currentChannel;
    private List<ChannelMessageDto> _currentChannelMessages = new();
    private Dictionary<Guid, int> _unreadCounts = new();

    public event Action? OnChange;

    public List<ChannelDto> MyChannels
    {
        get => _myChannels;
        set
        {
            _myChannels = value;
            NotifyStateChanged();
        }
    }

    public ChannelDetailsDto? CurrentChannel
    {
        get => _currentChannel;
        set
        {
            _currentChannel = value;
            NotifyStateChanged();
        }
    }

    public List<ChannelMessageDto> CurrentChannelMessages
    {
        get => _currentChannelMessages;
        set
        {
            _currentChannelMessages = value;
            NotifyStateChanged();
        }
    }

    public Dictionary<Guid, int> UnreadCounts
    {
        get => _unreadCounts;
        set
        {
            _unreadCounts = value;
            NotifyStateChanged();
        }
    }

    public void AddMessage(ChannelMessageDto message)
    {
        if (_currentChannel != null && message.ChannelId == _currentChannel.Id)
        {
            _currentChannelMessages.Insert(0, message);
            NotifyStateChanged();
        }
    }

    public void UpdateMessage(Guid messageId, string newContent)
    {
        var message = _currentChannelMessages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            var index = _currentChannelMessages.IndexOf(message);
            _currentChannelMessages[index] = message with
            {
                Content = newContent,
                IsEdited = true,
                EditedAtUtc = DateTime.UtcNow
            };
            NotifyStateChanged();
        }
    }

    public void DeleteMessage(Guid messageId)
    {
        var message = _currentChannelMessages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            _currentChannelMessages.Remove(message);
            NotifyStateChanged();
        }
    }

    public void SetUnreadCount(Guid channelId, int count)
    {
        _unreadCounts[channelId] = count;
        NotifyStateChanged();
    }

    public int GetUnreadCount(Guid channelId)
    {
        return _unreadCounts.ContainsKey(channelId) ? _unreadCounts[channelId] : 0;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
