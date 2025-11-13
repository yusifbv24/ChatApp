namespace ChatApp.Blazor.Client.Infrastructure.SignalR;

/// <summary>
/// Interface for SignalR real-time event handling
/// </summary>
public interface ISignalRService
{
    event Action<Guid>? OnUserOnline;
    event Action<Guid>? OnUserOffline;
    event Action<object>? OnNewMessage;
    event Action<Guid, Guid>? OnMessageEdited;
    event Action<Guid, Guid>? OnMessageDeleted;
    event Action<Guid, string>? OnUserTyping;

    Task InitializeAsync();
    Task DisconnectAsync();
    Task<bool> IsConnectedAsync();
}
