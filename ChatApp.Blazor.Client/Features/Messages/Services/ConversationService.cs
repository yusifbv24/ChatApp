using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Services
{
    public class ConversationService(IApiClient _apiClient) : IConversationService
    {


        public async Task<Result> DeleteMessageAsync(Guid conversationId, Guid messageId)
        {
            return await _apiClient.DeleteAsync(
                $"/api/conversations/{conversationId}/messages/{messageId}");
        }



        public async Task<Result> EditMessageAsync(Guid conversationId, Guid messageId, string newContent)
        {
            return await _apiClient.PutAsync(
                $"/api/conversations/{conversationId}/messages/{messageId}",
                new EditMessageRequests { NewContent = newContent });
        }



        public async Task<Result<List<DirectConversationDto>>> GetConversationsAsync()
        {
            return await _apiClient.GetAsync<List<DirectConversationDto>>("/api/conversations");
        }



        public async Task<Result<List<DirectMessageDto>>> GetMessagesAsync(
            Guid conversationId, 
            int pageSize = 50, 
            DateTime? before = null)
        {
            var url = $"/api/conversations/{conversationId}/messages?pageSize={pageSize}";

            if (before.HasValue)
            {
                // Ensure DateTime is UTC and properly formatted for API
                var beforeUtc = DateTime.SpecifyKind(before.Value, DateTimeKind.Utc);
                url += $"&before={beforeUtc:O}"; // ISO 8601 format with timezone
            }

            return await _apiClient.GetAsync<List<DirectMessageDto>>(url);
        }


        public async Task<Result<List<DirectMessageDto>>> GetMessagesAroundAsync(
            Guid conversationId,
            Guid messageId,
            int count = 50)
        {
            return await _apiClient.GetAsync<List<DirectMessageDto>>(
                $"/api/conversations/{conversationId}/messages/around/{messageId}?count={count}");
        }


        public async Task<Result<int>> GetUnreadCountAsync(Guid conversationId)
        {
            var response = await _apiClient.GetAsync<UnreadCountResponse>(
                $"/api/conversations/{conversationId}/messages/unread-count");

            if(response.IsSuccess && response.Value != null)
            {
                return Result.Success(response.Value.UnreadCount);
            }

            return Result.Failure<int>(response.Error ?? "Failed to get unread count");
        }



        public async Task<Result> MarkAsReadAsync(Guid conversationId, Guid messageId)
        {
            return await _apiClient.PostAsync(
                $"/api/conversations/{conversationId}/messages/{messageId}/read");
        }


        public async Task<Result> MarkAllAsReadAsync(Guid conversationId)
        {
            return await _apiClient.PostAsync(
                $"/api/conversations/{conversationId}/messages/mark-as-read");
        }


        public async Task<Result<ReactionToggleResponse>> ToggleReactionAsync(Guid conversationId, Guid messageId, string reaction)
        {
            return await _apiClient.PutAsync<ReactionToggleResponse>(
                $"/api/conversations/{conversationId}/messages/{messageId}/reactions/toggle",
                new ReactionRequest { Reaction = reaction });
        }


        public async Task<Result> ToggleMessageAsLaterAsync(Guid conversationId, Guid messageId)
        {
            return await _apiClient.PostAsync(
                $"/api/conversations/{conversationId}/messages/{messageId}/mark-later/toggle");
        }



        public async Task<Result<List<DirectMessageDto>>> GetPinnedMessagesAsync(Guid conversationId)
        {
            return await _apiClient.GetAsync<List<DirectMessageDto>>(
                $"/api/conversations/{conversationId}/messages/pinned");
        }



        public async Task<Result<List<FavoriteDirectMessageDto>>> GetFavoriteMessagesAsync(Guid conversationId)
        {
            return await _apiClient.GetAsync<List<FavoriteDirectMessageDto>>(
                $"/api/conversations/{conversationId}/messages/favorites");
        }



        public async Task<Result> PinMessageAsync(Guid conversationId, Guid messageId)
        {
            return await _apiClient.PostAsync(
                $"/api/conversations/{conversationId}/messages/{messageId}/pin");
        }



        public async Task<Result> UnpinMessageAsync(Guid conversationId, Guid messageId)
        {
            return await _apiClient.DeleteAsync(
                $"/api/conversations/{conversationId}/messages/{messageId}/pin");
        }



        public async Task<Result<Guid>> SendMessageAsync(Guid conversationId, string content, string? fileId = null, Guid? replyToMessageId = null, bool isForwarded = false)
        {
            var response = await _apiClient.PostAsync<SendMessageResponse>(
                $"/api/conversations/{conversationId}/messages",
                new SendMessageRequests
                {
                    Content = content,
                    FileId = fileId,
                    ReplyToMessageId = replyToMessageId,
                    IsForwarded = isForwarded
                });

            if(response.IsSuccess && response.Value != null)
            {
                return Result.Success(response.Value.MessageId);
            }

            return Result.Failure<Guid>(response.Error ?? "Failed to send message");
        }



        public async Task<Result<Guid>> StartConversationAsync(Guid otherUserId)
        {
            var response = await _apiClient.PostAsync<StartConversationResponse>(
                "/api/conversations",
                new StartConversationRequests { OtherUserId = otherUserId });

            if(response.IsSuccess && response.Value != null)
            {
                return Result.Success(response.Value.ConversationId);
            }

            return Result.Failure<Guid>(response.Error ?? "Failed to start conversation");
        }


        public async Task<Result<bool>> ToggleFavoriteAsync(Guid conversationId, Guid messageId)
        {
            var result = await _apiClient.PostAsync<FavoriteToggleResponse>(
                $"/api/conversations/{conversationId}/messages/{messageId}/favorite/toggle");

            if (result.IsSuccess && result.Value != null)
            {
                return Result.Success(result.Value.IsFavorite);
            }

            return Result.Failure<bool>(result.Error ?? "Failed to toggle favorite");
        }


        private record StartConversationResponse(Guid ConversationId, string Message);
        private record SendMessageResponse(Guid MessageId, string Message);
        private record UnreadCountResponse(int UnreadCount);
        private record FavoriteToggleResponse(bool IsFavorite, string Message);
    }
}