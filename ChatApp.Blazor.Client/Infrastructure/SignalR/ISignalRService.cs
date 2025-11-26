using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Infrastructure.SignalR;

/// <summary>
/// Interface for SignalR real-time event handling
/// </summary>
public interface ISignalRService
{
    // Connection state
    bool IsConnected { get; }
    event Action? OnConnected;
    event Action? OnDisconnected;
    event Action? OnReconnecting;
    event Action? OnReconnected;

    // Presence events
    event Action<Guid>? OnUserOnline;
    event Action<Guid>? OnUserOffline;

    // Direct message events
    event Action<DirectMessageDto>? OnNewDirectMessage;
    event Action<Guid, Guid>? OnDirectMessageEdited;  // conversationId, messageId
    event Action<Guid, Guid>? OnDirectMessageDeleted; // conversationId, messageId
    event Action<Guid, Guid, Guid, DateTime>? OnMessageRead; // conversationId, messageId, readBy, readAtUtc

    // Channel message events
    event Action<ChannelMessageDto>? OnNewChannelMessage;
    event Action<Guid, Guid>? OnChannelMessageEdited;  // channelId, messageId
    event Action<Guid, Guid>? OnChannelMessageDeleted; // channelId, messageId

    // Typing indicators
    event Action<Guid, Guid, bool>? OnUserTypingInChannel;      // channelId, userId, isTyping
    event Action<Guid, Guid, bool>? OnUserTypingInConversation; // conversationId, userId, isTyping

    // Reaction events
    event Action<Guid, Guid, Guid, string>? OnReactionAdded;   // channelId, messageId, userId, reaction
    event Action<Guid, Guid, Guid, string>? OnReactionRemoved; // channelId, messageId, userId, reaction

    // Connection management
    Task InitializeAsync();
    Task DisconnectAsync();
    Task<bool> IsConnectedAsync();

    // Channel/Conversation group management
    Task JoinChannelAsync(Guid channelId);
    Task LeaveChannelAsync(Guid channelId);
    Task JoinConversationAsync(Guid conversationId);
    Task LeaveConversationAsync(Guid conversationId);

    // Typing indicators
    Task SendTypingInChannelAsync(Guid channelId, bool isTyping);
    Task SendTypingInConversationAsync(Guid conversationId, bool isTyping);

    // Online status
    Task<Dictionary<Guid, bool>> GetOnlineStatusAsync(List<Guid> userIds);
}
