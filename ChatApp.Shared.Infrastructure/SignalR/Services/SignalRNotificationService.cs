using ChatApp.Shared.Infrastructure.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Shared.Infrastructure.SignalR.Services
{
    public class SignalRNotificationService:ISignalRNotificationService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<SignalRNotificationService> _logger;

        public SignalRNotificationService(
            IHubContext<ChatHub> hubContext,
            IConnectionManager connectionManager,
            ILogger<SignalRNotificationService> logger)
        {
            _hubContext= hubContext;
            _connectionManager= connectionManager;
            _logger= logger;
        }

        public async Task NotifyChannelMessageAsync(Guid channelId, object messageDto)
        {
            _logger?.LogDebug("Broadcasting new message to channel {ChannelId}", channelId);

            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("NewChannelMessage", messageDto);
        }


        public async Task NotifyDirectMessageAsync(Guid conversationId, Guid receiverId, object messageDto)
        {
            _logger?.LogDebug(
                "Sending direct message notification to user {ReceiverId} in conversation {ConversationId}",
                receiverId,
                conversationId);

            // Send to conversation group
            await _hubContext.Clients
                .Group($"conversation_{conversationId}")
                .SendAsync("NewDirectMessage", messageDto);

            // Also send directly to receiver's connections (in case they're not in the group yet)
            var receiverConnections = await _connectionManager.GetUserConnectionsAsync(receiverId);
            if (receiverConnections.Any())
            {
                await _hubContext.Clients
                    .Clients(receiverConnections)
                    .SendAsync("NewDirectMessage", messageDto);
            }
        }


        public async Task NotifyMessageDeletedAsync(Guid channelId, Guid messageId)
        {
            _logger?.LogDebug("Broadcasting message deletion for message {MessageId} in channel {ChannelId}",
                messageId, channelId);

            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("MessageDeleted", new { channelId, messageId });
        }


        public async Task NotifyMessageEditedAsync(Guid channelId, Guid messageId)
        {
            _logger?.LogDebug("Broadcasting message edit for message {MessageId} in channel {ChannelId}",
                messageId, channelId);

            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("MessageEdited", new { channelId, messageId });
        }


        public async Task NotifyMessageReadAsync(Guid conversationId, Guid messageId, Guid readBy)
        {
            _logger?.LogDebug("Broadcasting message read for message {MessageId}", messageId);

            await _hubContext.Clients
                .Group($"conversation_{conversationId}")
                .SendAsync("MessageRead", new { conversationId, messageId, readBy, readAtUtc = DateTime.UtcNow });
        }


        public async Task NotifyReactionAddedAsync(Guid channelId, Guid messageId, Guid userId, string reaction)
        {
            _logger?.LogDebug("Broadcasting reaction added to message {MessageId}", messageId);

            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ReactionAdded", new { channelId, messageId, userId, reaction });
        }


        public async Task NotifyReactionRemovedAsync(Guid channelId, Guid messageId, Guid userId, string reaction)
        {
            _logger?.LogDebug("Broadcasting reaction removed from message {MessageId}", messageId);

            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ReactionRemoved", new { channelId, messageId, userId, reaction });
        }


        public async Task NotifyUserAsync(Guid userId, string eventName, object data)
        {
            _logger?.LogDebug("Sending event {EventName} to user {UserId}", eventName, userId);

            var userConnections = await _connectionManager.GetUserConnectionsAsync(userId);

            if (userConnections.Any())
            {
                await _hubContext.Clients
                    .Clients(userConnections)
                    .SendAsync(eventName, data);
            }
        }
    }
}