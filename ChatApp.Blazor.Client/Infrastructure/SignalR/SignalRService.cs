namespace ChatApp.Blazor.Client.Infrastructure.SignalR
{
    public class SignalRService : ISignalRService
    {
        private readonly IChatHubConnection _hubConnection;
        private bool _isInitialized;
        public event Action<Guid>? OnUserOnline;
        public event Action<Guid>? OnUserOffline;
        public event Action<object>? OnNewMessage;
        public event Action<Guid, Guid>? OnMessageEdited;
        public event Action<Guid, Guid>? OnMessageDeleted;
        public event Action<Guid, string>? OnUserTyping;

        public SignalRService(IChatHubConnection hubConnection)
        {
            _hubConnection = hubConnection;
        }

        public async Task DisconnectAsync()
        {
            await _hubConnection.StopAsync();
            _isInitialized = false;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            await _hubConnection.StartAsync();

            // Register event handlers
            _hubConnection.On<Guid>("UserOnline", userId => OnUserOnline?.Invoke(userId));
            _hubConnection.On<Guid>("UserOffline", userId => OnUserOffline?.Invoke(userId));
            _hubConnection.On<object>("NewMessage", message => OnNewMessage?.Invoke(message));
            _hubConnection.On<Guid, Guid>("MessageEdited", (conversationId, messageId) =>
                OnMessageEdited?.Invoke(conversationId, messageId));
            _hubConnection.On<Guid, Guid>("MessageDeleted", (conversationId, messageId) =>
                OnMessageDeleted?.Invoke(conversationId, messageId));
            _hubConnection.On<Guid, string>("UserTyping", (userId, conversationId) =>
                OnUserTyping?.Invoke(userId, conversationId));

            _isInitialized = true;
        }

        public async Task<bool> IsConnectedAsync()
        {
            return await _hubConnection.IsConnectedAsync();
        }
    }
}