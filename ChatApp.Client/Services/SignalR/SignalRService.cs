using ChatApp.Client.Models.Channels;
using ChatApp.Client.Services.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatApp.Client.Services.SignalR
{
    public class SignalRService : ISignalRService
    {
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SignalRService> _logger;
        private HubConnection? _hubConnection;
        private bool _isStarting = false;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        public event Action<MessageDto>? OnMessageReceived;
        public event Action<MessageDto>? OnMessageEdited;
        public event Action<Guid>? OnMessageDeleted;
        public event Action<Guid, Guid, string>? OnUserTyping;
        public event Action<Guid, Guid>? OnUserStoppedTyping;

        public event Action<DirectMessageDto>? OnDirectMessageReceived;
        public event Action<DirectMessageDto>? OnDirectMessageEdited;
        public event Action<Guid>? OnDirectMessageDeleted;
        public event Action<Guid, Guid, string>? OnDirectMessageTyping;

        public event Action<Guid>? OnUserOnline;
        public event Action<Guid>? OnUserOffline;
        public event Action<Guid, string>? OnUserStatusChanged;

        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action? OnReconnecting;
        public event Action? OnReconnected;

        public SignalRService(
            ITokenService tokenService,
            IConfiguration configuration,
            ILogger<SignalRService> logger)
        {
            _tokenService = tokenService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Gets whether connection is established
        /// </summary>
        public bool IsConnected =>
            _hubConnection?.State == HubConnectionState.Connected;

        /// <summary>
        /// Gets current connection state as string
        /// </summary>
        public string ConnectionState =>
            _hubConnection?.State.ToString() ?? "Disconnected";


        public async Task StartAsync()
        {
            // Prevent multiple simultaneous starts
            await _connectionLock.WaitAsync();
            try
            {
                // If already connected or starting, don't start again
                if (_isStarting || IsConnected)
                {
                    _logger.LogInformation("SignalR already starting or connected");
                    return;
                }

                _isStarting = true;
                _logger.LogInformation("Starting SignalR connection...");

                // Get configuration
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"]
                    ?? throw new InvalidOperationException("API Base URL not configured");
                var hubPath = _configuration["ApiSettings:SignalRHubUrl"] ?? "/hubs/chat";
                var hubUrl = $"{apiBaseUrl}{hubPath}";

                // Get JWT token for authentication
                var accessToken = await _tokenService.GetAccessTokenAsync();
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    _logger.LogWarning("No access token available for SignalR connection");
                    return;
                }

                // Build the connection
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        // Attach JWT token to connection
                        // This is sent in the initial WebSocket handshake
                        options.AccessTokenProvider = async () => await Task.FromResult(accessToken);

                        // Configure transport (prefer WebSockets, fallback to others)
                        options.SkipNegotiation = false; // Let SignalR negotiate best transport
                    })
                    .WithAutomaticReconnect(new RetryPolicy()) // Custom reconnect policy
                    .ConfigureLogging(logging =>
                    {
                        // Enable SignalR logging for debugging
                        logging.SetMinimumLevel(LogLevel.Information);
                    })
                    .Build();

                // Register all event handlers
                RegisterEventHandlers();

                // Register connection lifecycle handlers
                _hubConnection.Closed += OnConnectionClosed;
                _hubConnection.Reconnecting += OnConnectionReconnecting;
                _hubConnection.Reconnected += OnConnectionReconnected;

                // Start the connection
                await _hubConnection.StartAsync();

                _logger.LogInformation("SignalR connection established successfully");
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start SignalR connection");
                throw;
            }
            finally
            {
                _isStarting = false;
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// Registers all event handlers for server-to-client events
        /// Each .On() method subscribes to a server event by name
        /// </summary>
        private void RegisterEventHandlers()
        {
            if (_hubConnection == null) return;

            // Channel message events
            _hubConnection.On<MessageDto>("ReceiveMessage", (message) =>
            {
                _logger.LogDebug("Received message: {MessageId}", message.Id);
                OnMessageReceived?.Invoke(message);
            });

            _hubConnection.On<MessageDto>("MessageEdited", (message) =>
            {
                _logger.LogDebug("Message edited: {MessageId}", message.Id);
                OnMessageEdited?.Invoke(message);
            });

            _hubConnection.On<Guid>("MessageDeleted", (messageId) =>
            {
                _logger.LogDebug("Message deleted: {MessageId}", messageId);
                OnMessageDeleted?.Invoke(messageId);
            });

            _hubConnection.On<Guid, Guid, string>("UserTyping", (channelId, userId, username) =>
            {
                _logger.LogDebug("User typing: {Username} in channel {ChannelId}", username, channelId);
                OnUserTyping?.Invoke(channelId, userId, username);
            });

            _hubConnection.On<Guid, Guid>("UserStoppedTyping", (channelId, userId) =>
            {
                OnUserStoppedTyping?.Invoke(channelId, userId);
            });

            // Direct message events
            _hubConnection.On<DirectMessageDto>("ReceiveDirectMessage", (message) =>
            {
                _logger.LogDebug("Received direct message: {MessageId}", message.Id);
                OnDirectMessageReceived?.Invoke(message);
            });

            _hubConnection.On<DirectMessageDto>("DirectMessageEdited", (message) =>
            {
                OnDirectMessageEdited?.Invoke(message);
            });

            _hubConnection.On<Guid>("DirectMessageDeleted", (messageId) =>
            {
                OnDirectMessageDeleted?.Invoke(messageId);
            });

            _hubConnection.On<Guid, Guid, string>("DirectMessageTyping", (conversationId, userId, username) =>
            {
                OnDirectMessageTyping?.Invoke(conversationId, userId, username);
            });

            // User presence events
            _hubConnection.On<Guid>("UserConnected", (userId) =>
            {
                _logger.LogDebug("User online: {UserId}", userId);
                OnUserOnline?.Invoke(userId);
            });

            _hubConnection.On<Guid>("UserDisconnected", (userId) =>
            {
                _logger.LogDebug("User offline: {UserId}", userId);
                OnUserOffline?.Invoke(userId);
            });

            _hubConnection.On<Guid, string>("UserStatusChanged", (userId, status) =>
            {
                _logger.LogDebug("User status changed: {UserId} -> {Status}", userId, status);
                OnUserStatusChanged?.Invoke(userId, status);
            });
        }

        /// <summary>
        /// Handles connection closed event
        /// </summary>
        private Task OnConnectionClosed(Exception? exception)
        {
            _logger.LogWarning(exception, "SignalR connection closed");
            OnDisconnected?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles reconnecting event
        /// </summary>
        private Task OnConnectionReconnecting(Exception? exception)
        {
            _logger.LogWarning(exception, "SignalR reconnecting...");
            OnReconnecting?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles reconnected event
        /// </summary>
        private Task OnConnectionReconnected(string? connectionId)
        {
            _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
            OnReconnected?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the SignalR connection gracefully
        /// Call this on logout or app close
        /// </summary>
        public async Task StopAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (_hubConnection != null)
                {
                    _logger.LogInformation("Stopping SignalR connection...");

                    // Unregister lifecycle handlers
                    _hubConnection.Closed -= OnConnectionClosed;
                    _hubConnection.Reconnecting -= OnConnectionReconnecting;
                    _hubConnection.Reconnected -= OnConnectionReconnected;

                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;

                    _logger.LogInformation("SignalR connection stopped");
                    OnDisconnected?.Invoke();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping SignalR connection");
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        // ====================================================================
        // CLIENT-TO-SERVER METHODS
        // These invoke methods on the server hub
        // ====================================================================

        /// <summary>
        /// Joins a channel to receive its messages
        /// Server adds this connection to the channel's group
        /// </summary>
        public async Task JoinChannelAsync(Guid channelId)
        {
            if (_hubConnection == null || !IsConnected)
            {
                _logger.LogWarning("Cannot join channel - not connected");
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("JoinChannel", channelId);
                _logger.LogInformation("Joined channel: {ChannelId}", channelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join channel: {ChannelId}", channelId);
            }
        }

        /// <summary>
        /// Leaves a channel
        /// Server removes this connection from the channel's group
        /// </summary>
        public async Task LeaveChannelAsync(Guid channelId)
        {
            if (_hubConnection == null || !IsConnected)
                return;

            try
            {
                await _hubConnection.InvokeAsync("LeaveChannel", channelId);
                _logger.LogInformation("Left channel: {ChannelId}", channelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave channel: {ChannelId}", channelId);
            }
        }

        /// <summary>
        /// Sends typing indicator to server
        /// Server broadcasts to other channel members
        /// </summary>
        public async Task SendTypingIndicatorAsync(Guid channelId)
        {
            if (_hubConnection == null || !IsConnected)
                return;

            try
            {
                await _hubConnection.InvokeAsync("SendTypingIndicator", channelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send typing indicator");
            }
        }

        public async Task JoinConversationAsync(Guid conversationId)
        {
            if (_hubConnection == null || !IsConnected)
                return;

            try
            {
                await _hubConnection.InvokeAsync("JoinConversation", conversationId);
                _logger.LogInformation("Joined conversation: {ConversationId}", conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join conversation");
            }
        }

        public async Task LeaveConversationAsync(Guid conversationId)
        {
            if (_hubConnection == null || !IsConnected)
                return;

            try
            {
                await _hubConnection.InvokeAsync("LeaveConversation", conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave conversation");
            }
        }

        public async Task SendDirectMessageTypingIndicatorAsync(Guid conversationId)
        {
            if (_hubConnection == null || !IsConnected)
                return;

            try
            {
                await _hubConnection.InvokeAsync("SendDirectMessageTypingIndicator", conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send DM typing indicator");
            }
        }

        public async Task UpdateStatusAsync(string status)
        {
            if (_hubConnection == null || !IsConnected)
                return;

            try
            {
                await _hubConnection.InvokeAsync("UpdateStatus", status);
                _logger.LogInformation("Updated status to: {Status}", status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update status");
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _connectionLock.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Custom retry policy for automatic reconnection
    /// Implements exponential backoff: 0s, 2s, 5s, 10s, 30s, 60s (max)
    /// </summary>
    internal class RetryPolicy : IRetryPolicy
    {
        private readonly TimeSpan[] _retryDelays = new[]
        {
            TimeSpan.Zero,              // First retry immediately
            TimeSpan.FromSeconds(2),    // Then wait 2 seconds
            TimeSpan.FromSeconds(5),    // Then 5 seconds
            TimeSpan.FromSeconds(10),   // Then 10 seconds
            TimeSpan.FromSeconds(30),   // Then 30 seconds
            TimeSpan.FromSeconds(60)    // Then 60 seconds (max)
        };

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            // If we've exceeded max attempts, stop retrying (return null)
            if (retryContext.PreviousRetryCount >= _retryDelays.Length)
                return null;

            // Return the delay for this attempt
            return _retryDelays[retryContext.PreviousRetryCount];
        }
    }
}