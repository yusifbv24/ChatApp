using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Services
{
    public interface IConversationService
    {
        /// <summary>
        /// İstifadəçi axtarışı (conversation başlatmaq üçün).
        /// </summary>
        Task<Result<List<UserSearchResultDto>>> SearchUsersAsync(string query);

        Task<Result<UnifiedConversationListResponse>> GetUnifiedListAsync(int pageNumber = 1, int pageSize = 20);

        Task<Result<Guid>> StartConversationAsync(Guid otherUserId);


        Task<Result<List<DirectMessageDto>>> GetMessagesAsync(Guid conversationId, int pageSize = 30, DateTime? before = null);


        Task<Result<List<DirectMessageDto>>> GetMessagesAroundAsync(Guid conversationId, Guid messageId, int count = 30);


        Task<Result<List<DirectMessageDto>>> GetMessagesBeforeAsync(Guid conversationId, DateTime beforeUtc, int limit = 100);


        Task<Result<List<DirectMessageDto>>> GetMessagesAfterAsync(Guid conversationId, DateTime afterUtc, int limit = 100);


        Task<Result<int>> GetUnreadCountAsync(Guid conversationId);


        Task<Result<Guid>> SendMessageAsync(Guid conversationId, string content, string? fileId = null, Guid? replyToMessageId = null, bool isForwarded = false, Dictionary<string, Guid>? mentionedUsers = null);

        /// <summary>
        /// Sends multiple messages in a single batch (for multi-file uploads).
        /// </summary>
        Task<Result<List<Guid>>> SendBatchMessagesAsync(Guid conversationId, BatchSendMessagesRequest request);


        Task<Result> EditMessageAsync(Guid conversationId, Guid messageId, string newContent);


        Task<Result> DeleteMessageAsync(Guid conversationId,Guid messageId);
        Task<Result> BatchDeleteMessagesAsync(Guid conversationId, List<Guid> messageIds);


        Task<Result> MarkAsReadAsync(Guid conversationId, Guid messageId);


        Task<Result> MarkAllAsReadAsync(Guid conversationId);


        Task<Result<ReactionToggleResponse>> ToggleReactionAsync(Guid conversationId, Guid messageId, string reaction);


        Task<Result> ToggleMessageAsLaterAsync(Guid conversationId, Guid messageId);


        Task<Result<List<DirectMessageDto>>> GetPinnedMessagesAsync(Guid conversationId);


        Task<Result<List<FavoriteDirectMessageDto>>> GetFavoriteMessagesAsync(Guid conversationId);


        Task<Result> PinMessageAsync(Guid conversationId, Guid messageId);


        Task<Result> UnpinMessageAsync(Guid conversationId, Guid messageId);


        Task<Result<bool>> ToggleFavoriteAsync(Guid conversationId, Guid messageId);


        Task<Result<bool>> TogglePinConversationAsync(Guid conversationId);


        Task<Result<bool>> ToggleMuteConversationAsync(Guid conversationId);


        Task<Result<bool>> ToggleMarkConversationAsReadLaterAsync(Guid conversationId);


        /// <summary>
        /// Marks all unread messages as read AND clears all "mark as read later" flags.
        /// Returns the count of messages marked as read.
        /// </summary>
        Task<Result<int>> MarkAllMessagesAsReadAsync(Guid conversationId);


        /// <summary>
        /// Clears all "mark as read later" flags (both conversation-level and message-level) when opening conversation.
        /// Does NOT mark messages as read - only removes the icon from conversation list.
        /// </summary>
        Task<Result> UnmarkConversationReadLaterAsync(Guid conversationId);


        /// <summary>
        /// Hides a conversation from the list. It will reappear when a new message arrives.
        /// </summary>
        Task<Result> HideConversationAsync(Guid conversationId);
    }
}