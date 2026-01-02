using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Services
{
    public interface IConversationService
    {
        Task<Result<List<DirectConversationDto>>> GetConversationsAsync();


        Task<Result<Guid>> StartConversationAsync(Guid otherUserId);


        Task<Result<List<DirectMessageDto>>> GetMessagesAsync(Guid conversationId, int pageSize = 50, DateTime? before = null);


        Task<Result<int>> GetUnreadCountAsync(Guid conversationId);


        Task<Result<Guid>> SendMessageAsync(Guid conversationId, string content, string? fileId = null, Guid? replyToMessageId = null, bool isForwarded = false);


        Task<Result> EditMessageAsync(Guid conversationId, Guid messageId, string newContent);


        Task<Result> DeleteMessageAsync(Guid conversationId,Guid messageId);


        Task<Result> MarkAsReadAsync(Guid conversationId, Guid messageId);


        Task<Result> MarkAllAsReadAsync(Guid conversationId);


        Task<Result<ReactionToggleResponse>> ToggleReactionAsync(Guid conversationId, Guid messageId, string reaction);


        Task<Result> ToggleMessageAsLaterAsync(Guid conversationId, Guid messageId);


        Task<Result<List<DirectMessageDto>>> GetPinnedMessagesAsync(Guid conversationId);


        Task<Result<List<FavoriteDirectMessageDto>>> GetFavoriteMessagesAsync(Guid conversationId);


        Task<Result> PinMessageAsync(Guid conversationId, Guid messageId);


        Task<Result> UnpinMessageAsync(Guid conversationId, Guid messageId);


        Task<Result<bool>> ToggleFavoriteAsync(Guid conversationId, Guid messageId);
    }
}