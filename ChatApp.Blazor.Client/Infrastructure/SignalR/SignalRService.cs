using ChatApp.Blazor.Client.Models.Messages;
using System.Text.Json;

namespace ChatApp.Blazor.Client.Infrastructure.SignalR;

/// <summary>
/// Service for handling SignalR real-time events
/// </summary>
public class SignalRService(IChatHubConnection hubConnection) : ISignalRService
{
    private readonly List<IDisposable> _subscriptions = [];
    private bool _isInitialized;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };


    // Connection state
    public bool IsConnected { get; private set; }
    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action? OnReconnecting;
    public event Action? OnReconnected;


    // Presence events
    public event Action<Guid>? OnUserOnline;
    public event Action<Guid>? OnUserOffline;


    // Direct message events
    public event Action<DirectMessageDto>? OnNewDirectMessage;
    public event Action<Guid, Guid>? OnDirectMessageEdited;
    public event Action<Guid, Guid>? OnDirectMessageDeleted;
    public event Action<Guid, Guid, Guid, DateTime>? OnMessageRead;


    // Channel message events
    public event Action<ChannelMessageDto>? OnNewChannelMessage;
    public event Action<Guid, Guid>? OnChannelMessageEdited;
    public event Action<Guid, Guid>? OnChannelMessageDeleted;


    // Typing indicators
    public event Action<Guid, Guid, bool>? OnUserTypingInChannel;
    public event Action<Guid, Guid, bool>? OnUserTypingInConversation;


    // Reaction events
    public event Action<Guid, Guid, Guid, string>? OnReactionAdded;
    public event Action<Guid, Guid, Guid, string>? OnReactionRemoved;

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await hubConnection.StartAsync();
        IsConnected=true;
        _isInitialized=true;

        RegisterEventHandlers();
        OnConnected?.Invoke();
    }


    private void RegisterEventHandlers()
    {
        // Presence events
        _subscriptions.Add(hubConnection.On<Guid>("UserOnline", userId =>
        {
            OnUserOnline?.Invoke(userId);
        }));

        _subscriptions.Add(hubConnection.On<Guid>("UserOffline", userId =>
        {
            OnUserOffline?.Invoke(userId);
        }));

        // Direct message events
        _subscriptions.Add(hubConnection.On<object>("NewDirectMessage", messageObj =>
        {
            try
            {
                var json = JsonSerializer.Serialize(messageObj);
                var message = JsonSerializer.Deserialize<DirectMessageDto>(json, _jsonOptions);
                if (message != null)
                {
                    OnNewDirectMessage?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing direct message: {ex.Message}");
            }
        }));

        // Channel message events
        _subscriptions.Add(hubConnection.On<object>("NewChannelMessage", messageObj =>
        {
            try
            {
                var json = JsonSerializer.Serialize(messageObj);
                var message = JsonSerializer.Deserialize<ChannelMessageDto>(json, _jsonOptions);
                if (message != null)
                {
                    OnNewChannelMessage?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing channel message: {ex.Message}");
            }
        }));

        // Message edited events
        _subscriptions.Add(hubConnection.On<object>("MessageEdited", data =>
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("channelId", out var channelIdProp) &&
                   root.TryGetProperty("messageId", out var messageIdProp))
                {
                    var channelId = channelIdProp.GetGuid();
                    var messageId = messageIdProp.GetGuid();
                    OnChannelMessageEdited?.Invoke(channelId, messageId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message edited: {ex.Message}");
            }
        }));

        // Message deleted events
        _subscriptions.Add(hubConnection.On<object>("MessageDeleted", data =>
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("channelId", out var channelIdProp) &&
                   root.TryGetProperty("messageId", out var messageIdProp))
                {
                    var channelId = channelIdProp.GetGuid();
                    var messageId = messageIdProp.GetGuid();
                    OnChannelMessageDeleted?.Invoke(channelId, messageId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message deleted: {ex.Message}");
            }
        }));

        // Message read events
        _subscriptions.Add(hubConnection.On<object>("MessageRead", data =>
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("conversationId", out var convIdProp) &&
                   root.TryGetProperty("messageId", out var msgIdProp) &&
                   root.TryGetProperty("readBy", out var readByProp) &&
                   root.TryGetProperty("readAtUtc", out var readAtProp))
                {
                    var conversationId = convIdProp.GetGuid();
                    var messageId = msgIdProp.GetGuid();
                    var readBy = readByProp.GetGuid();
                    var readAtUtc = readAtProp.GetDateTime();
                    OnMessageRead?.Invoke(conversationId, messageId, readBy, readAtUtc);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message read: {ex.Message}");
            }
        }));

        // Typing indicators
        _subscriptions.Add(hubConnection.On<Guid, Guid, bool>("UserTypingInChannel",
            (channelId, userId, isTyping) =>
            {
                OnUserTypingInChannel?.Invoke(channelId, userId, isTyping);
            }));

        _subscriptions.Add(hubConnection.On<Guid, Guid, bool>("UserTypingInConversation",
            (conversationId, userId, IsTyping) =>
            {
                OnUserTypingInConversation?.Invoke(conversationId, userId, IsTyping);
            }));

        // Reaction events
        _subscriptions.Add(hubConnection.On<object>("ReactionAdded", data =>
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("channelId", out var channelIdProp) &&
                   root.TryGetProperty("messageId", out var messageIdProp) &&
                   root.TryGetProperty("userId", out var userIdProp) &&
                   root.TryGetProperty("reaction", out var reactionProp))
                {
                    var channelId = channelIdProp.GetGuid();
                    var messageId = messageIdProp.GetGuid();
                    var userId = userIdProp.GetGuid();
                    var reaction = reactionProp.GetString() ?? "";
                    OnReactionAdded?.Invoke(channelId, messageId, userId, reaction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing reaction added: {ex.Message}");
            }
        }));

        _subscriptions.Add(hubConnection.On<object>("ReactionRemoved", data =>
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("channelId", out var channelIdProp) &&
                   root.TryGetProperty("messageId", out var messageIdProp) &&
                   root.TryGetProperty("userId", out var userIdProp) &&
                   root.TryGetProperty("reaction", out var reactionProp))
                {
                    var channelId = channelIdProp.GetGuid();
                    var messageId = messageIdProp.GetGuid();
                    var userId = userIdProp.GetGuid();
                    var reaction = reactionProp.GetString() ?? "";
                    OnReactionRemoved?.Invoke(channelId, messageId, userId, reaction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing reaction removed: {ex.Message}");
            }
        }));
    }

    public async Task DisconnectAsync()
    {
        foreach(var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();

        await hubConnection.StopAsync();
        IsConnected=false;
        _isInitialized = false;
        OnDisconnected?.Invoke();
    }

    public Task<bool> IsConnectedAsync()
    {
        return hubConnection.IsConnectedAsync();
    }

    public async Task JoinChannelAsync(Guid channelId)
    {
        if (_isInitialized)
        {
            await hubConnection.SendMessageAsync("JoinChannel", channelId);
        }
    }

    public async Task LeaveChannelAsync(Guid channelId)
    {
        if (_isInitialized)
        {
            await hubConnection.SendMessageAsync("LeaveChannel", channelId);
        }    
    }
    
    public async Task JoinConversationAsync(Guid conversationId)
    {
        if (_isInitialized)
        {
            await hubConnection.SendMessageAsync("JoinConversation", conversationId);
        }
    }

    public async Task LeaveConversationAsync(Guid conversationId)
    {
        if (_isInitialized)
        {
            await hubConnection.SendMessageAsync("LeaveConversation", conversationId);
        }
    }

    public async Task SendTypingInChannelAsync(Guid channelId,bool isTyping)
    {
        if (_isInitialized)
        {
            await hubConnection.SendMessageAsync("TypingInChannel",channelId,isTyping);
        }
    }

    public async Task SendTypingInConversationAsync(Guid conversationId,bool isTyping)
    {
        if (_isInitialized)
        {
            await hubConnection.SendMessageAsync("TypingInConversation",conversationId,isTyping);
        }
    }

    public async Task<Dictionary<Guid, bool>> GetOnlineStatusAsync(List<Guid> userIds)
    {
        // For now, return empty - this would require invoke support
        return await Task.FromResult(new Dictionary<Guid, bool>());
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}