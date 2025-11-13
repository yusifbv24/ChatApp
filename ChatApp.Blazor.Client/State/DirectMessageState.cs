using ChatApp.Blazor.Client.Models.DirectMessages;

namespace ChatApp.Blazor.Client.State;

/// <summary>
/// State management for direct messages and conversations
/// </summary>
public class DirectMessageState
{
    private List<DirectConversationDto> _conversations = new();
    private DirectConversationDto? _currentConversation;
    private List<DirectMessageDto> _currentMessages = new();
    private Dictionary<Guid, int> _unreadCounts = new();

    public event Action? OnChange;

    public IReadOnlyList<DirectConversationDto> Conversations => _conversations;
    public DirectConversationDto? CurrentConversation => _currentConversation;
    public IReadOnlyList<DirectMessageDto> CurrentMessages => _currentMessages;

    /// <summary>
    /// Sets all conversations
    /// </summary>
    public void SetConversations(List<DirectConversationDto> conversations)
    {
        _conversations = conversations;

        // Update unread counts
        _unreadCounts.Clear();
        foreach (var conv in conversations)
        {
            if (conv.UnreadCount > 0)
            {
                _unreadCounts[conv.Id] = conv.UnreadCount;
            }
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the current active conversation
    /// </summary>
    public void SetCurrentConversation(DirectConversationDto? conversation)
    {
        _currentConversation = conversation;

        if (conversation != null)
        {
            // Clear unread count for this conversation
            _unreadCounts.Remove(conversation.Id);
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Sets messages for the current conversation
    /// </summary>
    public void SetCurrentMessages(List<DirectMessageDto> messages)
    {
        _currentMessages = messages;
        NotifyStateChanged();
    }

    /// <summary>
    /// Adds a new message to the current conversation
    /// </summary>
    public void AddMessage(DirectMessageDto message)
    {
        if (_currentConversation != null && message.ConversationId == _currentConversation.Id)
        {
            // Add to beginning (messages are ordered newest first)
            _currentMessages.Insert(0, message);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Updates an existing message
    /// </summary>
    public void UpdateMessage(DirectMessageDto updatedMessage)
    {
        var index = _currentMessages.FindIndex(m => m.Id == updatedMessage.Id);
        if (index >= 0)
        {
            _currentMessages[index] = updatedMessage;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Removes a message
    /// </summary>
    public void RemoveMessage(Guid messageId)
    {
        var index = _currentMessages.FindIndex(m => m.Id == messageId);
        if (index >= 0)
        {
            _currentMessages.RemoveAt(index);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Updates unread count for a conversation
    /// </summary>
    public void UpdateUnreadCount(Guid conversationId, int count)
    {
        if (count > 0)
        {
            _unreadCounts[conversationId] = count;
        }
        else
        {
            _unreadCounts.Remove(conversationId);
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Gets unread count for a conversation
    /// </summary>
    public int GetUnreadCount(Guid conversationId)
    {
        return _unreadCounts.GetValueOrDefault(conversationId, 0);
    }

    /// <summary>
    /// Gets total unread count across all conversations
    /// </summary>
    public int GetTotalUnreadCount()
    {
        return _unreadCounts.Values.Sum();
    }

    /// <summary>
    /// Updates a conversation in the list (e.g., when last message changes)
    /// </summary>
    public void UpdateConversation(DirectConversationDto conversation)
    {
        var index = _conversations.FindIndex(c => c.Id == conversation.Id);
        if (index >= 0)
        {
            _conversations[index] = conversation;
        }
        else
        {
            _conversations.Insert(0, conversation);
        }

        // Re-sort by last message time
        _conversations = _conversations
            .OrderByDescending(c => c.LastMessageAtUtc)
            .ToList();

        NotifyStateChanged();
    }

    /// <summary>
    /// Clears all state
    /// </summary>
    public void Clear()
    {
        _conversations.Clear();
        _currentConversation = null;
        _currentMessages.Clear();
        _unreadCounts.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
