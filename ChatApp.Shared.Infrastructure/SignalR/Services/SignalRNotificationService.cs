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

        public async Task NotifyChannelMessageToMembersAsync(Guid channelId, List<Guid> memberUserIds, object messageDto)
        {
            _logger?.LogDebug("Broadcasting new message to channel {ChannelId} and {MemberCount} members directly",
                channelId, memberUserIds.Count);

            // 1. Send to channel group (for users actively viewing the channel)
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("NewChannelMessage", messageDto);

            // 2. ALSO send directly to each member's connections (for lazy loading support)
            // This ensures notifications work even if user hasn't joined the channel group yet
            // OPTIMIZATION: Batch all connections and send once instead of N individual sends
            var allConnections = new List<string>();
            foreach (var memberId in memberUserIds)
            {
                var memberConnections = await _connectionManager.GetUserConnectionsAsync(memberId);
                allConnections.AddRange(memberConnections);
            }

            if (allConnections.Count > 0)
            {
                await _hubContext.Clients
                    .Clients(allConnections)
                    .SendAsync("NewChannelMessage", messageDto);
            }
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
            if (receiverConnections.Count != 0)
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

        public async Task NotifyDirectMessageEditedAsync(Guid conversationId, Guid receiverId, object messageDto)
        {
            _logger?.LogDebug(
                "Sending edited direct message notification to user {ReceiverId} in conversation {ConversationId}",
                receiverId,
                conversationId);

            // Send to conversation group
            await _hubContext.Clients
                .Group($"conversation_{conversationId}")
                .SendAsync("DirectMessageEdited", messageDto);

            // Also send directly to receiver's connections
            var receiverConnections = await _connectionManager.GetUserConnectionsAsync(receiverId);
            if (receiverConnections.Count != 0)
            {
                await _hubContext.Clients
                    .Clients(receiverConnections)
                    .SendAsync("DirectMessageEdited", messageDto);
            }
        }

        public async Task NotifyChannelMessageEditedAsync(Guid channelId, object messageDto)
        {
            _logger?.LogDebug("Broadcasting edited channel message to channel {ChannelId}", channelId);

            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ChannelMessageEdited", messageDto);
        }

        public async Task NotifyChannelMessageEditedToMembersAsync(Guid channelId, List<Guid> memberUserIds, object messageDto)
        {
            _logger?.LogDebug("Broadcasting edited message to channel {ChannelId} and {MemberCount} members directly",
                channelId, memberUserIds.Count);

            // 1. Send to channel group (for active viewers)
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ChannelMessageEdited", messageDto);

            // 2. Send directly to each member's connections (for lazy loading)
            // OPTIMIZATION: Batch all connections
            var allConnections = new List<string>();
            foreach (var memberId in memberUserIds)
            {
                var memberConnections = await _connectionManager.GetUserConnectionsAsync(memberId);
                allConnections.AddRange(memberConnections);
            }

            if (allConnections.Count > 0)
            {
                await _hubContext.Clients
                    .Clients(allConnections)
                    .SendAsync("ChannelMessageEdited", messageDto);
            }
        }

        public async Task NotifyDirectMessageDeletedAsync(Guid conversationId, Guid receiverId, object messageDto)
        {
            _logger?.LogDebug(
                "Sending deleted direct message notification to user {ReceiverId} in conversation {ConversationId}",
                receiverId,
                conversationId);

            // Send to conversation group
            await _hubContext.Clients
                .Group($"conversation_{conversationId}")
                .SendAsync("DirectMessageDeleted", messageDto);

            // Also send directly to receiver's connections
            var receiverConnections = await _connectionManager.GetUserConnectionsAsync(receiverId);
            if (receiverConnections.Count != 0)
            {
                await _hubContext.Clients
                    .Clients(receiverConnections)
                    .SendAsync("DirectMessageDeleted", messageDto);
            }
        }

        public async Task NotifyChannelMessageDeletedAsync(Guid channelId, object messageDto)
        {
            _logger?.LogDebug("Broadcasting deleted channel message to channel {ChannelId}", channelId);

            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ChannelMessageDeleted", messageDto);
        }

        public async Task NotifyChannelMessageDeletedToMembersAsync(Guid channelId, List<Guid> memberUserIds, object messageDto)
        {
            _logger?.LogDebug("Broadcasting deleted message to channel {ChannelId} and {MemberCount} members directly",
                channelId, memberUserIds.Count);

            // 1. Send to channel group (for active viewers)
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ChannelMessageDeleted", messageDto);

            // 2. Send directly to each member's connections (for lazy loading)
            // OPTIMIZATION: Batch all connections
            var allConnections = new List<string>();
            foreach (var memberId in memberUserIds)
            {
                var memberConnections = await _connectionManager.GetUserConnectionsAsync(memberId);
                allConnections.AddRange(memberConnections);
            }

            if (allConnections.Count > 0)
            {
                await _hubContext.Clients
                    .Clients(allConnections)
                    .SendAsync("ChannelMessageDeleted", messageDto);
            }
        }

        public async Task NotifyMessageReadAsync(Guid conversationId, Guid messageId, Guid readBy, Guid senderId)
        {
            _logger?.LogDebug("Broadcasting message read for message {MessageId} to sender {SenderId}", messageId, senderId);

            var notification = new { conversationId, messageId, readBy };

            // Send to conversation group (both users if they're in the group)
            await _hubContext.Clients
                .Group($"conversation_{conversationId}")
                .SendAsync("MessageRead", notification);

            // ALSO send directly to sender specifically
            // This ensures sender gets notification even if they're not actively in the conversation group
            var senderConnections = await _connectionManager.GetUserConnectionsAsync(senderId);
            if (senderConnections.Count != 0)
            {
                await _hubContext.Clients
                    .Clients(senderConnections)
                    .SendAsync("MessageRead", notification);
            }
        }

        public async Task NotifyChannelMessagesReadAsync(Guid channelId, Guid userId, Dictionary<Guid, int> messageReadCounts)
        {
            _logger?.LogDebug("Broadcasting {Count} messages read for user {UserId} in channel {ChannelId}",
                messageReadCounts.Count, userId, channelId);

            // Broadcast to all members in the channel group
            // Send messageId -> readByCount pairs so conversation list can update status correctly
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ChannelMessagesRead", channelId, userId, messageReadCounts);
        }

        public async Task NotifyChannelMessagesReadToMembersAsync(Guid channelId, List<Guid> memberUserIds, Guid userId, Dictionary<Guid, int> messageReadCounts)
        {
            _logger?.LogDebug("Broadcasting {Count} messages read for user {UserId} to channel {ChannelId} and {MemberCount} members directly",
                messageReadCounts.Count, userId, channelId, memberUserIds.Count);

            // 1. Send to channel group (for users actively viewing the channel)
            await NotifyChannelMessagesReadAsync(channelId, userId, messageReadCounts);

            // 2. ALSO send directly to each member's connections (for lazy loading support)
            // This ensures users who left the channel (and left the group) still receive the update
            // OPTIMIZATION: Batch all connections and send once instead of N individual sends
            var allConnections = new List<string>();
            foreach (var memberId in memberUserIds)
            {
                var connections = await _connectionManager.GetUserConnectionsAsync(memberId);
                allConnections.AddRange(connections);
            }

            if (allConnections.Count > 0)
            {
                // Send to all member connections in a single operation
                await _hubContext.Clients
                    .Clients(allConnections)
                    .SendAsync("ChannelMessagesRead", channelId, userId, messageReadCounts);
            }
        }


        public async Task NotifyReactionAddedAsync(Guid channelId, Guid messageId, Guid userId, string reaction)
        {
            _logger?.LogDebug("Broadcasting reaction added to message {MessageId}", messageId);

            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ReactionAdded", channelId, messageId, userId, reaction);
        }


        public async Task NotifyReactionRemovedAsync(Guid channelId, Guid messageId, Guid userId, string reaction)
        {
            _logger?.LogDebug("Broadcasting reaction removed from message {MessageId}", messageId);

            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ReactionRemoved", channelId, messageId, userId, reaction);
        }

        public async Task NotifyChannelMessageReactionsUpdatedAsync(Guid channelId, Guid messageId, object reactions)
        {
            _logger?.LogDebug("Broadcasting reactions updated for message {MessageId} in channel {ChannelId}",
                messageId, channelId);

            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ChannelMessageReactionsUpdated", new { messageId, reactions });
        }

        public async Task NotifyChannelMessageReactionsUpdatedToMembersAsync(Guid channelId, List<Guid> memberUserIds, Guid messageId, object reactions)
        {
            _logger?.LogDebug("Broadcasting reactions updated for message {MessageId} to channel {ChannelId} and {MemberCount} members directly",
                messageId, channelId, memberUserIds.Count);

            // 1. Send to channel group (for users actively viewing the channel - real-time)
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("ChannelMessageReactionsUpdated", new { messageId, reactions });

            // 2. ALSO send directly to each member's connections (for lazy loading support)
            // This ensures reactions propagate even if user hasn't joined the channel group yet
            // OPTIMIZATION: Batch all connections
            var allConnections = new List<string>();
            foreach (var memberId in memberUserIds)
            {
                var memberConnections = await _connectionManager.GetUserConnectionsAsync(memberId);
                allConnections.AddRange(memberConnections);
            }

            if (allConnections.Count > 0)
            {
                await _hubContext.Clients
                    .Clients(allConnections)
                    .SendAsync("ChannelMessageReactionsUpdated", new { messageId, reactions });
            }
        }


        public async Task NotifyUserAsync(Guid userId, string eventName, object data)
        {
            _logger?.LogDebug("Sending event {EventName} to user {UserId}", eventName, userId);

            var userConnections = await _connectionManager.GetUserConnectionsAsync(userId);

            if (userConnections.Count != 0)
            {
                await _hubContext.Clients
                    .Clients(userConnections)
                    .SendAsync(eventName, data);
            }
        }


        public async Task NotifyMemberAddedToChannelAsync(Guid userId, object channelDto)
        {
            _logger?.LogDebug("Notifying user {UserId} about being added to channel", userId);

            var userConnections = await _connectionManager.GetUserConnectionsAsync(userId);

            if (userConnections.Count != 0)
            {
                await _hubContext.Clients
                    .Clients(userConnections)
                    .SendAsync("AddedToChannel", channelDto);
            }
        }

        public async Task NotifyMemberLeftChannelAsync(Guid channelId, Guid leftUserId, string leftUserFullName)
        {
            _logger?.LogDebug("Broadcasting member left to channel {ChannelId} group. Left user: {LeftUserId}",
                channelId, leftUserId);

            var notification = new
            {
                channelId,
                leftUserId,
                leftUserFullName
            };

            // Send only to channel group (no hybrid pattern needed)
            // Members who left the group won't need this notification
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("MemberLeftChannel", notification);
        }


        public async Task NotifyUserTypingInChannelToMembersAsync(Guid channelId, List<Guid> memberUserIds, Guid typingUserId, string fullName, bool isTyping)
        {
            _logger?.LogDebug("Broadcasting typing indicator to channel {ChannelId} and {MemberCount} members directly",
                channelId, memberUserIds.Count);

            // 1. Send to channel group (for users actively viewing the channel - real-time, no delay)
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .SendAsync("UserTypingInChannel", channelId, typingUserId, fullName, isTyping);

            // 2. ALSO send directly to each member's connections (for lazy loading support)
            // This allows typing indicators to appear in conversation list even if user hasn't joined the channel
            // OPTIMIZATION: Batch all connections
            var allConnections = new List<string>();
            foreach (var memberId in memberUserIds)
            {
                var memberConnections = await _connectionManager.GetUserConnectionsAsync(memberId);
                allConnections.AddRange(memberConnections);
            }

            if (allConnections.Count > 0)
            {
                await _hubContext.Clients
                    .Clients(allConnections)
                    .SendAsync("UserTypingInChannel", channelId, typingUserId, fullName, isTyping);
            }
        }


        public async Task NotifyUserTypingInConversationToMembersAsync(Guid conversationId, List<Guid> memberUserIds, Guid typingUserId, bool isTyping)
        {
            _logger?.LogDebug("Broadcasting typing indicator to conversation {ConversationId} and {MemberCount} members directly",
                conversationId, memberUserIds.Count);

            // 1. Send to conversation group (for active viewers - real-time)
            await _hubContext.Clients
                .Group($"conversation_{conversationId}")
                .SendAsync("UserTypingInConversation", conversationId, typingUserId, isTyping);

            // 2. ALSO send directly to each member's connections (for lazy loading)
            // OPTIMIZATION: Batch all connections
            var allConnections = new List<string>();
            foreach (var memberId in memberUserIds)
            {
                var memberConnections = await _connectionManager.GetUserConnectionsAsync(memberId);
                allConnections.AddRange(memberConnections);
            }

            if (allConnections.Count > 0)
            {
                await _hubContext.Clients
                    .Clients(allConnections)
                    .SendAsync("UserTypingInConversation", conversationId, typingUserId, isTyping);
            }
        }
    }
}