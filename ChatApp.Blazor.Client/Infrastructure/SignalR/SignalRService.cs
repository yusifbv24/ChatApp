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
    public event Action<DirectMessageDto>? OnDirectMessageEdited;
    public event Action<DirectMessageDto>? OnDirectMessageDeleted;
    public event Action<Guid, Guid, Guid, DateTime>? OnMessageRead;


    // Channel message events
    public event Action<ChannelMessageDto>? OnNewChannelMessage;
    public event Action<ChannelMessageDto>? OnChannelMessageEdited;
    public event Action<ChannelMessageDto>? OnChannelMessageDeleted;


    // Typing indicators
    public event Action<Guid, Guid, bool>? OnUserTypingInChannel;
    public event Action<Guid, Guid, bool>? OnUserTypingInConversation;


    // Reaction events
    public event Action<Guid, Guid, List<ReactionSummary>>? OnDirectMessageReactionToggled;

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await hubConnection.StartAsync();
        IsConnected=true;
        _isInitialized=true;

        RegisterEventHandlers();
        RegisterConnectionEvents();
        OnConnected?.Invoke();
    }

    private void RegisterConnectionEvents()
    {
        // Register connection lifecycle events from the hub connection
        hubConnection.Reconnecting += async (error) =>
        {
            IsConnected = false;
            await Task.Run(() => OnReconnecting?.Invoke());
        };

        hubConnection.Reconnected += async (connectionId) =>
        {
            IsConnected = true;
            await Task.Run(() => OnReconnected?.Invoke());
        };

        hubConnection.Closed += async (error) =>
        {
            IsConnected = false;
            await Task.Run(() => OnDisconnected?.Invoke());
        };
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

        // Direct message edited event
        _subscriptions.Add(hubConnection.On<object>("DirectMessageEdited", messageObj =>
        {
            try
            {
                var json = JsonSerializer.Serialize(messageObj);
                var message = JsonSerializer.Deserialize<DirectMessageDto>(json, _jsonOptions);
                if (message != null)
                {
                    OnDirectMessageEdited?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing edited direct message: {ex.Message}");
            }
        }));

        // Direct message deleted event
        _subscriptions.Add(hubConnection.On<object>("DirectMessageDeleted", messageObj =>
        {
            try
            {
                var json = JsonSerializer.Serialize(messageObj);
                var message = JsonSerializer.Deserialize<DirectMessageDto>(json, _jsonOptions);
                if (message != null)
                {
                    OnDirectMessageDeleted?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing deleted direct message: {ex.Message}");
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

        // Channel message edited event
        _subscriptions.Add(hubConnection.On<object>("ChannelMessageEdited", messageObj =>
        {
            try
            {
                var json = JsonSerializer.Serialize(messageObj);
                var message = JsonSerializer.Deserialize<ChannelMessageDto>(json, _jsonOptions);
                if (message != null)
                {
                    OnChannelMessageEdited?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing edited channel message: {ex.Message}");
            }
        }));

        // Channel message deleted event
        _subscriptions.Add(hubConnection.On<object>("ChannelMessageDeleted", messageObj =>
        {
            try
            {
                var json = JsonSerializer.Serialize(messageObj);
                var message = JsonSerializer.Deserialize<ChannelMessageDto>(json, _jsonOptions);
                if (message != null)
                {
                    OnChannelMessageDeleted?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing deleted channel message: {ex.Message}");
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
        _subscriptions.Add(hubConnection.On<object>("DirectMessageReactionToggled", data =>
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if(root.TryGetProperty("conversationId", out var conversationIdProp) &&
                   root.TryGetProperty("messageId", out var messageIdProp) &&
                   root.TryGetProperty("reactions", out var reactionsProp))
                {
                    var conversationId = conversationIdProp.GetGuid();
                    var messageId = messageIdProp.GetGuid();

                    var reactions = new List<ReactionSummary>();
                    if (reactionsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var reactionElement in reactionsProp.EnumerateArray())
                        {
                            var emoji = reactionElement.GetProperty("emoji").GetString() ?? "";
                            var count = reactionElement.GetProperty("count").GetInt32();
                            var userIds = new List<Guid>();

                            if (reactionElement.TryGetProperty("userIds", out var userIdsProp) &&
                                userIdsProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var userIdElement in userIdsProp.EnumerateArray())
                                {
                                    userIds.Add(userIdElement.GetGuid());
                                }
                            }

                            reactions.Add(new ReactionSummary
                            {
                                Emoji = emoji,
                                Count = count,
                                UserIds = userIds
                            });
                        }
                    }

                    OnDirectMessageReactionToggled?.Invoke(conversationId, messageId, reactions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing reaction toggled: {ex.Message}");
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
        if (!_isInitialized)
        {
            return [];
        }

        try
        {
            return await hubConnection.InvokeAsync<Dictionary<Guid, bool>>("GetOnlineStatus", userIds);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting online status: {ex.Message}");
            return [];
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}