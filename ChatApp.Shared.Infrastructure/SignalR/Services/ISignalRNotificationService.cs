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
        /// Notify about reaction added
        /// </summary>
        Task NotifyReactionAddedAsync(Guid channelId, Guid messageId, Guid userId, string reaction);



        /// <summary>
        /// Notify about reaction removed
        /// </summary>
        Task NotifyReactionRemovedAsync(Guid channelId, Guid messageId, Guid userId, string reaction);



        /// <summary>
        /// Notify about message read (for direct messages)
        /// Sends notification to both conversation group AND sender specifically
        /// </summary>
        Task NotifyMessageReadAsync(Guid conversationId, Guid messageId, Guid readBy, Guid senderId, DateTime readAtUtc);


        /// <summary>
        /// Notify about channel messages read
        /// Broadcasts when a member updates their LastReadAtUtc timestamp
        /// </summary>
        Task NotifyChannelMessagesReadAsync(Guid channelId, Guid userId, DateTime readAtUtc);


        /// <summary>
        /// Notify specific user about something
        /// </summary>
        Task NotifyUserAsync(Guid userId, string eventName, object data);


        /// <summary>
        /// Notify user that they have been added to a channel
        /// </summary>
        Task NotifyMemberAddedToChannelAsync(Guid userId, object channelDto);
    }
}