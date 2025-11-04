using ChatApp.Shared.Infrastructure.SignalR.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChatApp.Shared.Infrastructure.SignalR.Hubs
{
    /// <summary>
    /// Main SignalR hub for real-time chat functionality
    /// </summary>
    [Authorize]
    public class ChatHub:Hub
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IPresenceService _presenceService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IConnectionManager connectionManager,
            IPresenceService presenceService,
            ILogger<ChatHub> logger)
        {
            _connectionManager= connectionManager;
            _presenceService= presenceService;
            _logger= logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();

            if (userId != Guid.Empty)
            {
                await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);
                await _presenceService.UserConnectedAsync(userId, Context.ConnectionId);

                _logger?.LogInformation(
                    "User {UserId} connected with ConnectionId {ConnectionId}",
                    userId,
                    Context.ConnectionId);

                // Notify all clients about user coming online
                await Clients.All.SendAsync("UserOnline", userId);
            }
            await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();

            if(userId!= Guid.Empty)
            {
                await _connectionManager.RemoveConnectionAsync(Context.ConnectionId);
                await _presenceService.UserDisconnectedAsync(Context.ConnectionId);

                _logger?.LogInformation(
                    "User {UserId} disconnected from ConnectionId {ConnectionId}",
                    userId,
                    Context.ConnectionId);

                // Check if user is still online on other devices
                var isStillOnline = await _connectionManager.IsUserOnlineAsync(userId);

                if (!isStillOnline)
                {
                    // Notify all clients about user going offline
                    await Clients.All.SendAsync("UserOffline", userId);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }


        /// <summary>
        /// Client notifies they are typing in a channel
        /// </summary>
        public async Task TypingInChannel(Guid channelId,bool isTyping)
        {
            var userId = GetUserId();

            if (userId == Guid.Empty)
                return;

            _logger?.LogDebug(
                "User {UserId} is {Status} in channel {ChannelId}",
                userId,
                isTyping ? "typing" : "stopped typing",
                channelId);

            // Broadcast to all users in the channel except sender
            await Clients.Group($"channel_{channelId}").SendAsync(
                "UserTypingInChannel",
                channelId,
                userId,
                isTyping);
        }


        /// <summary>
        /// Client notifies they are typing in a direct conversation
        /// </summary>
        public async Task TypingInConversation(Guid conversationId,bool isTyping)
        {
            var userId = GetUserId();

            if(userId==Guid.Empty) return;

            _logger?.LogDebug(
                "User {UserId} is {Status} in conversation {ConversationId}",
                userId,
                isTyping ? "typing" : "stopped typing",
                conversationId);

            // Broadcast to all users in the conversation except sender
            await Clients.Group($"conversation_{conversationId}").SendAsync(
                "UserTypingInConversation",
                conversationId,
                userId,
                isTyping);
        }


        /// <summary>
        /// Join a channel group for real-time updates
        /// </summary>
        public async Task JoinChannel(Guid channelId)
        {
            var userId = GetUserId();

            if(userId == Guid.Empty) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel_{channelId}");

            _logger?.LogInformation(
                "User {UserId} joined channel group {ChannelId}",
                userId,
                channelId);
        }



        /// <summary>
        /// Leave a channel group
        /// </summary>
        public async Task LeaveChannel(Guid channelId)
        {
            var userId = GetUserId();

            if(userId == Guid.Empty) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channelId}");

            _logger?.LogInformation(
                "User {UserId} left channel group {ChannelId}",
                userId,
                channelId);
        }



        /// <summary>
        /// Join a conversation group for real-time updates
        /// </summary>
        public async Task JoinConversation(Guid conversationId)
        {
            var userId = GetUserId();

            if(userId == Guid.Empty) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");

            _logger?.LogInformation(
                "User {UserId} joined conversation group {ConversationId}",
                userId,
                conversationId);
        }


        /// <summary>
        /// Leave a conversation group
        /// </summary>
        public async Task LeaveConversation(Guid conversationId)
        {
            var userId = GetUserId();

            if (userId == Guid.Empty)
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");

            _logger.LogInformation(
                "User {UserId} left conversation group {ConversationId}",
                userId,
                conversationId);
        }



        /// <summary>
        /// Get online status for a list of users
        /// </summary>
        public async Task<Dictionary<Guid, bool>> GetOnlineStatus(List<Guid> userIds)
        {
            return await _presenceService.GetUsersOnlineStatusAsync(userIds);
        }


        private Guid GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Guid.Empty;
            }

            return userId;
        }
    }
}