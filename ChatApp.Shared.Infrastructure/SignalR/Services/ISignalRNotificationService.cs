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
        /// Notify about message deletion
        /// </summary>
        Task NotifyMessageDeletedAsync(Guid channelId, Guid messageId);



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
        /// </summary>
        Task NotifyMessageReadAsync(Guid conversationId, Guid messageId, Guid readBy);



        /// <summary>
        /// Notify specific user about something
        /// </summary>
        Task NotifyUserAsync(Guid userId, string eventName, object data);
    }
}