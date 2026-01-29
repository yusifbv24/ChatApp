using ChatApp.Shared.Infrastructure.SignalR.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly IChannelMemberCache _channelMemberCache;

        public ChatHub(
            IConnectionManager connectionManager,
            IPresenceService presenceService,
            ISignalRNotificationService signalRNotificationService,
            IChannelMemberCache channelMemberCache)
        {
            _connectionManager= connectionManager;
            _presenceService= presenceService;
            _signalRNotificationService = signalRNotificationService;
            _channelMemberCache = channelMemberCache;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();

            if (userId != Guid.Empty)
            {
                await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);
                await _presenceService.UserConnectedAsync(userId, Context.ConnectionId);

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
        /// Uses hybrid pattern: broadcasts to group (for active viewers) AND direct connections (for lazy loading)
        /// </summary>
        public async Task TypingInChannel(Guid channelId,bool isTyping)
        {
            var userId = GetUserId();

            if (userId == Guid.Empty)
                return;

            var fullName = GetUsername(); // ClaimTypes.Name now contains FullName

            // Get channel members from cache
            var memberUserIds = await _channelMemberCache.GetChannelMemberIdsAsync(channelId);

            // Exclude sender from member list
            var recipientUserIds = memberUserIds.Where(id => id != userId).ToList();

            if (recipientUserIds.Any())
            {
                // HYBRID BROADCAST: Send to both group AND direct connections
                // This allows typing indicators to work even without JOIN (lazy loading)
                await _signalRNotificationService.NotifyUserTypingInChannelToMembersAsync(
                    channelId,
                    recipientUserIds,
                    userId,
                    fullName,
                    isTyping);
            }
            else
            {
                // Fallback: If cache is empty, just broadcast to group (backward compatible)
                // Cache will be populated on next message send or member change
                await Clients.Group($"channel_{channelId}").SendAsync(
                    "UserTypingInChannel",
                    channelId,
                    userId,
                    fullName,
                    isTyping);
            }
        }


        /// <summary>
        /// Client notifies they are typing in a direct conversation
        /// Uses hybrid pattern: broadcasts to group AND directly to recipient
        /// </summary>
        public async Task TypingInConversation(Guid conversationId, Guid recipientUserId, bool isTyping)
        {
            var userId = GetUserId();

            if (userId == Guid.Empty) return;

            // HYBRID BROADCAST: Send to both group AND direct connection
            // This allows typing indicators to work even without JOIN (lazy loading)
            await _signalRNotificationService.NotifyUserTypingInConversationToMembersAsync(
                conversationId,
                new List<Guid> { recipientUserId },
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
        }



        /// <summary>
        /// Leave a channel group
        /// </summary>
        public async Task LeaveChannel(Guid channelId)
        {
            var userId = GetUserId();

            if(userId == Guid.Empty) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channelId}");
        }



        /// <summary>
        /// Join a conversation group for real-time updates
        /// </summary>
        public async Task JoinConversation(Guid conversationId)
        {
            var userId = GetUserId();

            if(userId == Guid.Empty) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
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

        private string GetUsername()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Someone";
        }
    }
}