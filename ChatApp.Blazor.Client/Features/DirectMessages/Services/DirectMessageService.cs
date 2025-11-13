using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.DirectMessages;

namespace ChatApp.Blazor.Client.Features.DirectMessages.Services;

/// <summary>
/// Implementation of direct message service
/// Maps to: /api/conversations/{conversationId}/messages
/// </summary>
public class DirectMessageService : IDirectMessageService
{
    private readonly IApiClient _apiClient;

    public DirectMessageService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets messages in a conversation with pagination
    /// GET /api/conversations/{conversationId}/messages?pageSize={pageSize}&beforeUtc={beforeUtc}
    /// Requires: Messages.Read permission
    /// Returns messages ordered by creation time descending (newest first)
    /// </summary>
    public async Task<Result<List<DirectMessageDto>>> GetMessagesAsync(
        Guid conversationId,
        int pageSize = 50,
        DateTime? beforeUtc = null)
    {
        var beforeParam = beforeUtc.HasValue ? $"&beforeUtc={beforeUtc.Value:O}" : "";
        return await _apiClient.GetAsync<List<DirectMessageDto>>(
            $"/api/conversations/{conversationId}/messages?pageSize={pageSize}{beforeParam}");
    }

    /// <summary>
    /// Gets unread message count for a conversation
    /// GET /api/conversations/{conversationId}/messages/unread-count
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result<int>> GetUnreadCountAsync(Guid conversationId)
    {
        return await _apiClient.GetAsync<int>(
            $"/api/conversations/{conversationId}/messages/unread-count");
    }

    /// <summary>
    /// Sends a message in a conversation
    /// POST /api/conversations/{conversationId}/messages
    /// Requires: Messages.Send permission
    /// </summary>
    public async Task<Result<Guid>> SendMessageAsync(Guid conversationId, SendMessageRequest request)
    {
        return await _apiClient.PostAsync<Guid>(
            $"/api/conversations/{conversationId}/messages", request);
    }

    /// <summary>
    /// Edits a message (only sender can edit)
    /// PUT /api/conversations/{conversationId}/messages/{messageId}
    /// Requires: Messages.Edit permission
    /// </summary>
    public async Task<Result> EditMessageAsync(Guid conversationId, Guid messageId, EditMessageRequest request)
    {
        return await _apiClient.PutAsync(
            $"/api/conversations/{conversationId}/messages/{messageId}", request);
    }

    /// <summary>
    /// Deletes a message (soft delete, only sender can delete)
    /// DELETE /api/conversations/{conversationId}/messages/{messageId}
    /// Requires: Messages.Delete permission
    /// </summary>
    public async Task<Result> DeleteMessageAsync(Guid conversationId, Guid messageId)
    {
        return await _apiClient.DeleteAsync(
            $"/api/conversations/{conversationId}/messages/{messageId}");
    }

    /// <summary>
    /// Marks a message as read (only receiver can mark)
    /// POST /api/conversations/{conversationId}/messages/{messageId}/read
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result> MarkMessageAsReadAsync(Guid conversationId, Guid messageId)
    {
        return await _apiClient.PostAsync(
            $"/api/conversations/{conversationId}/messages/{messageId}/read", null);
    }

    /// <summary>
    /// Adds a reaction to a message
    /// POST /api/conversations/{conversationId}/messages/{messageId}/reactions
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result> AddReactionAsync(Guid conversationId, Guid messageId, AddReactionRequest request)
    {
        return await _apiClient.PostAsync(
            $"/api/conversations/{conversationId}/messages/{messageId}/reactions", request);
    }

    /// <summary>
    /// Removes a reaction from a message
    /// DELETE /api/conversations/{conversationId}/messages/{messageId}/reactions
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result> RemoveReactionAsync(Guid conversationId, Guid messageId, RemoveReactionRequest request)
    {
        return await _apiClient.DeleteAsync(
            $"/api/conversations/{conversationId}/messages/{messageId}/reactions", request);
    }
}
