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

    // Connection event handlers (stored for proper unsubscription)
    private Func<Exception?, Task>? _reconnectingHandler;
    private Func<string?, Task>? _reconnectedHandler;
    private Func<Exception?, Task>? _closedHandler;


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
    public event Action<Guid, Guid, Guid>? OnMessageRead;


    // Channel message events
    public event Action<ChannelMessageDto>? OnNewChannelMessage;
    public event Action<ChannelMessageDto>? OnChannelMessageEdited;
    public event Action<ChannelMessageDto>? OnChannelMessageDeleted;
    public event Action<Guid, Guid, Dictionary<Guid, int>>? OnChannelMessagesRead;


    // Typing indicators
    public event Action<Guid, Guid, string, bool>? OnUserTypingInChannel;
    public event Action<Guid, Guid, bool>? OnUserTypingInConversation;


    // Reaction events
    public event Action<Guid, Guid, List<ReactionSummary>>? OnDirectMessageReactionToggled;
    public event Action<Guid, Guid, Guid, string>? OnChannelReactionAdded;
    public event Action<Guid, Guid, Guid, string>? OnChannelReactionRemoved;
    public event Action<Guid, List<ChannelMessageReactionDto>>? OnChannelMessageReactionsUpdated;


    // Channel membership events
    public event Action<ChannelDto>? OnAddedToChannel;
    public event Action<Guid, Guid, string>? OnMemberLeftChannel;

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
        // Store handlers as fields for proper unsubscription in DisconnectAsync
        // FIX: Removed Task.Run - direct invocation is safer for UI thread operations
        _reconnectingHandler = (error) =>
        {
            IsConnected = false;
            OnReconnecting?.Invoke();
            return Task.CompletedTask;
        };
        hubConnection.Reconnecting += _reconnectingHandler;

        _reconnectedHandler = (connectionId) =>
        {
            IsConnected = true;
            OnReconnected?.Invoke();
            return Task.CompletedTask;
        };
        hubConnection.Reconnected += _reconnectedHandler;

        _closedHandler = (error) =>
        {
            IsConnected = false;
            OnDisconnected?.Invoke();
            return Task.CompletedTask;
        };
        hubConnection.Closed += _closedHandler;
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
            catch
            {
                // Silently handle deserialization errors
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
            catch
            {
                // Silently handle deserialization errors
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
            catch
            {
                // Silently handle deserialization errors
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
            catch
            {
                // Silently handle deserialization errors
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
            catch
            {
                // Silently handle deserialization errors
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
            catch
            {
                // Silently handle deserialization errors
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
                   root.TryGetProperty("readBy", out var readByProp))
                {
                    var conversationId = convIdProp.GetGuid();
                    var messageId = msgIdProp.GetGuid();
                    var readBy = readByProp.GetGuid();
                    OnMessageRead?.Invoke(conversationId, messageId, readBy);
                }
            }
            catch
            {
                // Silently handle message read processing errors
            }
        }));


        // Typing indicators
        _subscriptions.Add(hubConnection.On<Guid, Guid, string, bool>("UserTypingInChannel",
            (channelId, userId, fullName, isTyping) =>
            {
                OnUserTypingInChannel?.Invoke(channelId, userId, fullName, isTyping);
            }));

        _subscriptions.Add(hubConnection.On<Guid, Guid, bool>("UserTypingInConversation",
            (conversationId, userId, isTyping) =>
            {
                OnUserTypingInConversation?.Invoke(conversationId, userId, isTyping);
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
                            var userFullNames = new List<string>();
                            var userAvatarUrls = new List<string?>();

                            if (reactionElement.TryGetProperty("userIds", out var userIdsProp) &&
                                userIdsProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var userIdElement in userIdsProp.EnumerateArray())
                                {
                                    userIds.Add(userIdElement.GetGuid());
                                }
                            }

                            if (reactionElement.TryGetProperty("userFullNames", out var fullNamesProp) &&
                                fullNamesProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var nameElement in fullNamesProp.EnumerateArray())
                                {
                                    userFullNames.Add(nameElement.GetString() ?? "");
                                }
                            }

                            if (reactionElement.TryGetProperty("userAvatarUrls", out var avatarUrlsProp) &&
                                avatarUrlsProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var urlElement in avatarUrlsProp.EnumerateArray())
                                {
                                    userAvatarUrls.Add(urlElement.ValueKind == JsonValueKind.Null ? null : urlElement.GetString());
                                }
                            }

                            reactions.Add(new ReactionSummary
                            {
                                Emoji = emoji,
                                Count = count,
                                UserIds = userIds,
                                UserFullNames = userFullNames,
                                UserAvatarUrls = userAvatarUrls
                            });
                        }
                    }

                    OnDirectMessageReactionToggled?.Invoke(conversationId, messageId, reactions);
                }
            }
            catch
            {
                // Silently handle reaction processing errors
            }
        }));

        // Channel reaction events
        _subscriptions.Add(hubConnection.On<Guid, Guid, Guid, string>("ReactionAdded",
            (channelId, messageId, userId, reaction) =>
            {
                OnChannelReactionAdded?.Invoke(channelId, messageId, userId, reaction);
            }));

        _subscriptions.Add(hubConnection.On<Guid, Guid, Guid, string>("ReactionRemoved",
            (channelId, messageId, userId, reaction) =>
            {
                OnChannelReactionRemoved?.Invoke(channelId, messageId, userId, reaction);
            }));

        // Channel message reactions updated (simplified - replaces ReactionAdded/Removed)
        _subscriptions.Add(hubConnection.On<object>("ChannelMessageReactionsUpdated", data =>
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var payload = JsonSerializer.Deserialize<ChannelMessageReactionsUpdatedPayload>(json, _jsonOptions);

                if (payload != null && payload.MessageId != Guid.Empty)
                {
                    OnChannelMessageReactionsUpdated?.Invoke(payload.MessageId, payload.Reactions ?? new List<ChannelMessageReactionDto>());
                }
            }
            catch
            {
                // Silently handle deserialization errors
            }
        }));

        // Channel messages read event
        _subscriptions.Add(hubConnection.On<Guid, Guid, Dictionary<Guid, int>>("ChannelMessagesRead",
            (channelId, userId, messageReadCounts) =>
            {
                OnChannelMessagesRead?.Invoke(channelId, userId, messageReadCounts);
            }));


        // Channel membership - when user is added to a channel
        _subscriptions.Add(hubConnection.On<object>("AddedToChannel", channelObj =>
        {
            try
            {
                var json = JsonSerializer.Serialize(channelObj);
                var channel = JsonSerializer.Deserialize<ChannelDto>(json, _jsonOptions);
                if (channel != null)
                {
                    OnAddedToChannel?.Invoke(channel);
                }
            }
            catch
            {
                // Silently handle deserialization errors
            }
        }));

        // Channel membership - when a member leaves a channel
        _subscriptions.Add(hubConnection.On<object>("MemberLeftChannel", notificationObj =>
        {
            try
            {
                var json = JsonSerializer.Serialize(notificationObj);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var channelId = root.GetProperty("channelId").GetGuid();
                var leftUserId = root.GetProperty("leftUserId").GetGuid();
                var leftUserFullName = root.GetProperty("leftUserFullName").GetString() ?? "";

                OnMemberLeftChannel?.Invoke(channelId, leftUserId, leftUserFullName);
            }
            catch
            {
                // Silently handle deserialization errors
            }
        }));
    }

    public async Task DisconnectAsync()
    {
        // Unsubscribe from hub message events
        foreach(var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();

        // Unsubscribe from connection lifecycle events
        if (_reconnectingHandler != null)
        {
            hubConnection.Reconnecting -= _reconnectingHandler;
            _reconnectingHandler = null;
        }
        if (_reconnectedHandler != null)
        {
            hubConnection.Reconnected -= _reconnectedHandler;
            _reconnectedHandler = null;
        }
        if (_closedHandler != null)
        {
            hubConnection.Closed -= _closedHandler;
            _closedHandler = null;
        }

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

    public async Task SendTypingInConversationAsync(Guid conversationId, Guid recipientUserId, bool isTyping)
    {
        if (_isInitialized)
        {
            await hubConnection.SendMessageAsync("TypingInConversation", conversationId, recipientUserId, isTyping);
        }
    }

    public async Task<bool> IsUserOnlineAsync(Guid userId)
    {
        if (!_isInitialized)
        {
            return false;
        }

        try
        {
            // Backend List<Guid> qəbul edir, biz tək user göndəririk
            var result = await hubConnection.InvokeAsync<Dictionary<Guid, bool>>("GetOnlineStatus", new List<Guid> { userId });
            return result.TryGetValue(userId, out var isOnline) && isOnline;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}

// Payload class for ChannelMessageReactionsUpdated SignalR event
internal class ChannelMessageReactionsUpdatedPayload
{
    public Guid MessageId { get; set; }
    public List<ChannelMessageReactionDto>? Reactions { get; set; }
}