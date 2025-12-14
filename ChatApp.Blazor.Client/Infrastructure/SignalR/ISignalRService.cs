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
    event Action<DirectMessageDto>? OnDirectMessageEdited;
    event Action<DirectMessageDto>? OnDirectMessageDeleted;
    event Action<Guid, Guid, Guid, DateTime>? OnMessageRead;


    // Channel message events
    event Action<ChannelMessageDto>? OnNewChannelMessage;
    event Action<ChannelMessageDto>? OnChannelMessageEdited;
    event Action<ChannelMessageDto>? OnChannelMessageDeleted;


    // Typing indicators
    event Action<Guid, Guid, string, bool>? OnUserTypingInChannel;
    event Action<Guid, Guid, bool>? OnUserTypingInConversation;


    // Reaction events
    event Action<Guid, Guid, List<ReactionSummary>>? OnDirectMessageReactionToggled;


    // Channel membership events
    event Action<ChannelDto>? OnAddedToChannel;


    // Connection management
    Task InitializeAsync();
    Task DisconnectAsync();
    Task<bool> IsConnectedAsync();


    // Channel/Conversation group management
    Task JoinChannelAsync(Guid channelId);
    Task LeaveChannelAsync(Guid channelId);
    Task JoinConversationAsync(Guid channelId);
    Task LeaveConversationAsync(Guid conversationId);


    // Typing indicators
    Task SendTypingInChannelAsync(Guid channelId, bool isTyping);
    Task SendTypingInConversationAsync(Guid conversationId, bool isTyping);


    // Online status
    Task<Dictionary<Guid, bool>> GetOnlineStatusAsync(List<Guid> userIds);
}