using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Services
{
    public class ConversationService(IApiClient _apiClient) : IConversationService
    {
        /// <inheritdoc />
        public async Task<Result<List<UserSearchResultDto>>> SearchUsersAsync(string query)
        {
            return await _apiClient.GetAsync<List<UserSearchResultDto>>($"/api/users/search?q={Uri.EscapeDataString(query)}");
        }

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



        public async Task<Result<UnifiedConversationListResponse>> GetUnifiedListAsync(int pageNumber = 1, int pageSize = 20)
        {
            return await _apiClient.GetAsync<UnifiedConversationListResponse>($"/api/unified-conversations?pageNumber={pageNumber}&pageSize={pageSize}");
        }



        public async Task<Result<List<DirectMessageDto>>> GetMessagesAsync(
            Guid conversationId,
            int pageSize = 30,
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
            int count = 30)
        {
            return await _apiClient.GetAsync<List<DirectMessageDto>>(
                $"/api/conversations/{conversationId}/messages/around/{messageId}?count={count}");
        }


        public async Task<Result<List<DirectMessageDto>>> GetMessagesBeforeAsync(
            Guid conversationId,
            DateTime beforeUtc,
            int limit = 100)
        {
            // Ensure DateTime is UTC and properly formatted for API
            var beforeUtcSpecified = DateTime.SpecifyKind(beforeUtc, DateTimeKind.Utc);
            var url = $"/api/conversations/{conversationId}/messages/before?date={beforeUtcSpecified:O}&limit={limit}";
            return await _apiClient.GetAsync<List<DirectMessageDto>>(url);
        }


        public async Task<Result<List<DirectMessageDto>>> GetMessagesAfterAsync(
            Guid conversationId,
            DateTime afterUtc,
            int limit = 100)
        {
            // Ensure DateTime is UTC and properly formatted for API
            var afterUtcSpecified = DateTime.SpecifyKind(afterUtc, DateTimeKind.Utc);
            var url = $"/api/conversations/{conversationId}/messages/after?date={afterUtcSpecified:O}&limit={limit}";
            return await _apiClient.GetAsync<List<DirectMessageDto>>(url);
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



        /// <summary>
        /// Sends multiple messages in a single batch (for multi-file uploads).
        /// </summary>
        public async Task<Result<List<Guid>>> SendBatchMessagesAsync(Guid conversationId, BatchSendMessagesRequest request)
        {
            var response = await _apiClient.PostAsync<List<Guid>>(
                $"/api/conversations/{conversationId}/messages/batch",
                request);

            if (response.IsSuccess)
            {
                return Result.Success(response.Value!);
            }

            return Result.Failure<List<Guid>>(response.Error ?? "Failed to send batch messages");
        }

        public async Task<Result<Guid>> SendMessageAsync(
            Guid conversationId,
            string content,
            string? fileId = null,
            Guid? replyToMessageId = null,
            bool isForwarded = false,
            Dictionary<string, Guid>? mentionedUsers = null)
        {
            // Convert mentionedUsers dictionary to List<MentionRequest>
            var mentions = mentionedUsers?.Select(m => new MentionRequest
            {
                UserId = m.Value,
                UserName = m.Key,
                IsAllMention = false
            }).ToList() ?? new List<MentionRequest>();

            var response = await _apiClient.PostAsync<SendMessageResponse>(
                $"/api/conversations/{conversationId}/messages",
                new SendMessageRequests
                {
                    Content = content,
                    FileId = fileId,
                    ReplyToMessageId = replyToMessageId,
                    IsForwarded = isForwarded,
                    Mentions = mentions
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


        public async Task<Result<bool>> TogglePinConversationAsync(Guid conversationId)
        {
            var result = await _apiClient.PostAsync<PinToggleResponse>(
                $"/api/conversations/{conversationId}/messages/toggle-pin");

            if (result.IsSuccess && result.Value != null)
            {
                return Result.Success(result.Value.IsPinned);
            }

            return Result.Failure<bool>(result.Error ?? "Failed to toggle pin");
        }


        public async Task<Result<bool>> ToggleMuteConversationAsync(Guid conversationId)
        {
            var result = await _apiClient.PostAsync<MuteToggleResponse>(
                $"/api/conversations/{conversationId}/messages/toggle-mute");

            if (result.IsSuccess && result.Value != null)
            {
                return Result.Success(result.Value.IsMuted);
            }

            return Result.Failure<bool>(result.Error ?? "Failed to toggle mute");
        }


        public async Task<Result<bool>> ToggleMarkConversationAsReadLaterAsync(Guid conversationId)
        {
            var result = await _apiClient.PostAsync<ReadLaterToggleResponse>(
                $"/api/conversations/{conversationId}/messages/toggle-read-later");

            if (result.IsSuccess && result.Value != null)
            {
                return Result.Success(result.Value.IsMarkedReadLater);
            }

            return Result.Failure<bool>(result.Error ?? "Failed to toggle mark as read later");
        }


        /// <summary>
        /// Marks all unread messages as read AND clears message-level "mark as read later".
        /// Used when user clicks "Mark all as read" button in conversation more menu.
        /// </summary>
        public async Task<Result<int>> MarkAllMessagesAsReadAsync(Guid conversationId)
        {
            var result = await _apiClient.PostAsync<MarkAllReadResponse>(
                $"/api/conversations/{conversationId}/messages/mark-all-read");

            if (result.IsSuccess && result.Value != null)
            {
                return Result.Success(result.Value.MarkedCount);
            }

            return Result.Failure<int>(result.Error ?? "Failed to mark all messages as read");
        }


        /// <summary>
        /// Clears all "mark as read later" flags when user opens the conversation.
        /// Called automatically when user opens the conversation.
        /// Clears both IsMarkedReadLater and LastReadLaterMessageId.
        /// This removes the icon from conversation list but does NOT mark messages as read.
        /// </summary>
        public async Task<Result> UnmarkConversationReadLaterAsync(Guid conversationId)
        {
            return await _apiClient.DeleteAsync(
                $"/api/conversations/{conversationId}/messages/read-later");
        }


        /// <summary>
        /// Hides a conversation from the list. It will reappear when a new message arrives.
        /// </summary>
        public async Task<Result> HideConversationAsync(Guid conversationId)
        {
            return await _apiClient.PostAsync(
                $"/api/conversations/{conversationId}/messages/hide");
        }


        private record StartConversationResponse(Guid ConversationId, string Message);
        private record SendMessageResponse(Guid MessageId, string Message);
        private record UnreadCountResponse(int UnreadCount);
        private record FavoriteToggleResponse(bool IsFavorite, string Message);
        private record PinToggleResponse(bool IsPinned);
        private record MuteToggleResponse(bool IsMuted);
        private record ReadLaterToggleResponse(bool IsMarkedReadLater);
        private record MarkAllReadResponse(int MarkedCount, string Message);
    }
}