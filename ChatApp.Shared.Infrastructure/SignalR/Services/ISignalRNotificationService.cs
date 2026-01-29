namespace ChatApp.Shared.Infrastructure.SignalR.Services
{
    /// <summary>
    /// Service for sending real-time notifications via SignalR
    /// This is used by modules to broadcast events
    /// </summary>
    public interface ISignalRNotificationService
    {
        /// <summary>
        /// Notify channel about new message
        /// </summary>
        Task NotifyChannelMessageAsync(Guid channelId, object messageDto);

        /// <summary>
        /// Notify channel members about new message (hybrid: group + direct connections)
        /// Sends to channel group AND each member's connections directly
        /// </summary>
        Task NotifyChannelMessageToMembersAsync(Guid channelId, List<Guid> memberUserIds, object messageDto);


        /// <summary>
        /// Notify users in direct conversation about new message
        /// </summary>
        Task NotifyDirectMessageAsync(Guid conversationId,Guid receiverId,object messageDto);


        /// <summary>
        /// Notify about message edit
        /// </summary>
        Task NotifyMessageEditedAsync(Guid channelId, Guid messageId);

        /// <summary>
        /// Notify about direct message edit with updated content
        /// </summary>
        Task NotifyDirectMessageEditedAsync(Guid conversationId, Guid receiverId, object messageDto);

        /// <summary>
        /// Notify about channel message edit with updated content
        /// </summary>
        Task NotifyChannelMessageEditedAsync(Guid channelId, object messageDto);

        /// <summary>
        /// Notify channel members about edited message (hybrid: group + direct connections)
        /// </summary>
        Task NotifyChannelMessageEditedToMembersAsync(Guid channelId, List<Guid> memberUserIds, object messageDto);

        /// <summary>
        /// Notify about message deletion (old channel method - deprecated)
        /// </summary>
        Task NotifyMessageDeletedAsync(Guid channelId, Guid messageId);

        /// <summary>
        /// Notify about direct message deletion with updated DTO
        /// </summary>
        Task NotifyDirectMessageDeletedAsync(Guid conversationId, Guid receiverId, object messageDto);

        /// <summary>
        /// Notify about channel message deletion with updated DTO
        /// </summary>
        Task NotifyChannelMessageDeletedAsync(Guid channelId, object messageDto);

        /// <summary>
        /// Notify channel members about deleted message (hybrid: group + direct connections)
        /// </summary>
        Task NotifyChannelMessageDeletedToMembersAsync(Guid channelId, List<Guid> memberUserIds, object messageDto);



        /// <summary>
        /// Notify about reaction added
        /// </summary>
        Task NotifyReactionAddedAsync(Guid channelId, Guid messageId, Guid userId, string reaction);

        /// <summary>
        /// Notify about reaction removed
        /// </summary>
        Task NotifyReactionRemovedAsync(Guid channelId, Guid messageId, Guid userId, string reaction);

        /// <summary>
        /// Notify about channel message reactions updated (simplified - sends all reactions)
        /// </summary>
        Task NotifyChannelMessageReactionsUpdatedAsync(Guid channelId, Guid messageId, object reactions);

        /// <summary>
        /// Notify channel members about message reactions updated (hybrid: group + direct connections)
        /// Sends to channel group AND each member's connections directly
        /// </summary>
        Task NotifyChannelMessageReactionsUpdatedToMembersAsync(Guid channelId, List<Guid> memberUserIds, Guid messageId, object reactions);



        /// <summary>
        /// Notify about message read (for direct messages)
        /// Sends notification to both conversation group AND sender specifically
        /// </summary>
        Task NotifyMessageReadAsync(Guid conversationId, Guid messageId, Guid readBy, Guid senderId);


        /// <summary>
        /// Notify about channel messages read
        /// Broadcasts when a member marks messages as read, includes the message IDs and their current read counts
        /// </summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="userId">User who read the messages</param>
        /// <param name="messageReadCounts">Dictionary of message ID to current ReadByCount</param>
        Task NotifyChannelMessagesReadAsync(Guid channelId, Guid userId, Dictionary<Guid, int> messageReadCounts);

        /// <summary>
        /// Notify channel members about messages read (hybrid: group + direct connections)
        /// Sends to channel group AND each member's connections directly (for lazy loading support)
        /// </summary>
        Task NotifyChannelMessagesReadToMembersAsync(Guid channelId, List<Guid> memberUserIds, Guid userId, Dictionary<Guid, int> messageReadCounts);


        /// <summary>
        /// Notify specific user about something
        /// </summary>
        Task NotifyUserAsync(Guid userId, string eventName, object data);


        /// <summary>
        /// Notify user that they have been added to a channel
        /// </summary>
        Task NotifyMemberAddedToChannelAsync(Guid userId, object channelDto);

        /// <summary>
        /// Notify channel members that a member has left the channel
        /// Sends only to channel group (no hybrid pattern needed)
        /// </summary>
        Task NotifyMemberLeftChannelAsync(Guid channelId, Guid leftUserId, string leftUserFullName);


        /// <summary>
        /// Notify channel members about typing indicator (hybrid: group + direct connections)
        /// Sends typing indicator to channel group AND each member's connections directly
        /// This allows typing indicators to work with lazy loading (user doesn't need to JOIN channel)
        /// </summary>
        Task NotifyUserTypingInChannelToMembersAsync(Guid channelId, List<Guid> memberUserIds, Guid typingUserId, string fullName, bool isTyping);

        /// <summary>
        /// Notify conversation members about typing indicator (hybrid: group + direct connections)
        /// </summary>
        Task NotifyUserTypingInConversationToMembersAsync(Guid conversationId, List<Guid> memberUserIds, Guid typingUserId, bool isTyping);
    }
}