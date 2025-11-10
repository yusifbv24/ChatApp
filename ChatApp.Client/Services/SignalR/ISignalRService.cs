using ChatApp.Client.Models.Channels;

namespace ChatApp.Client.Services.SignalR
{
    public interface ISignalRService : IAsyncDisposable
    {
        Task StartAsync();

        Task StopAsync();

        bool IsConnected { get; }

        string ConnectionState { get; }

  
        event Action<MessageDto>? OnMessageReceived;

        event Action<MessageDto>? OnMessageEdited;

        event Action<Guid>? OnMessageDeleted;

        event Action<Guid, Guid, string>? OnUserTyping;

        event Action<Guid, Guid>? OnUserStoppedTyping;

        event Action<DirectMessageDto>? OnDirectMessageReceived;

        event Action<DirectMessageDto>? OnDirectMessageEdited;

        event Action<Guid>? OnDirectMessageDeleted;

        event Action<Guid, Guid, string>? OnDirectMessageTyping;

        event Action<Guid>? OnUserOnline;

        event Action<Guid>? OnUserOffline;

        event Action<Guid, string>? OnUserStatusChanged;

        event Action? OnConnected;

        event Action? OnDisconnected;

        event Action? OnReconnecting;

        event Action? OnReconnected;

        Task JoinChannelAsync(Guid channelId);

        Task LeaveChannelAsync(Guid channelId);

        Task SendTypingIndicatorAsync(Guid channelId);

        Task JoinConversationAsync(Guid conversationId);

        Task LeaveConversationAsync(Guid conversationId);

        Task SendDirectMessageTypingIndicatorAsync(Guid conversationId);

        Task UpdateStatusAsync(string status);
    }
}