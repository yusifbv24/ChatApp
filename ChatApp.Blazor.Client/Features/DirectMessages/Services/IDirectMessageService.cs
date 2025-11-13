using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.DirectMessages;

namespace ChatApp.Blazor.Client.Features.DirectMessages.Services;

/// <summary>
/// Interface for direct message management
/// </summary>
public interface IDirectMessageService
{
    /// <summary>
    /// Gets messages in a conversation with pagination
    /// GET /api/conversations/{conversationId}/messages?pageSize={pageSize}&beforeUtc={beforeUtc}
    /// </summary>
    Task<Result<List<DirectMessageDto>>> GetMessagesAsync(
        Guid conversationId,
        int pageSize = 50,
        DateTime? beforeUtc = null);

    /// <summary>
    /// Gets unread message count for a conversation
    /// GET /api/conversations/{conversationId}/messages/unread-count
    /// </summary>
    Task<Result<int>> GetUnreadCountAsync(Guid conversationId);

    /// <summary>
    /// Sends a message in a conversation
    /// POST /api/conversations/{conversationId}/messages
    /// </summary>
    Task<Result<Guid>> SendMessageAsync(Guid conversationId, SendMessageRequest request);

    /// <summary>
    /// Edits a message
    /// PUT /api/conversations/{conversationId}/messages/{messageId}
    /// </summary>
    Task<Result> EditMessageAsync(Guid conversationId, Guid messageId, EditMessageRequest request);

    /// <summary>
    /// Deletes a message
    /// DELETE /api/conversations/{conversationId}/messages/{messageId}
    /// </summary>
    Task<Result> DeleteMessageAsync(Guid conversationId, Guid messageId);

    /// <summary>
    /// Marks a message as read
    /// POST /api/conversations/{conversationId}/messages/{messageId}/read
    /// </summary>
    Task<Result> MarkMessageAsReadAsync(Guid conversationId, Guid messageId);

    /// <summary>
    /// Adds a reaction to a message
    /// POST /api/conversations/{conversationId}/messages/{messageId}/reactions
    /// </summary>
    Task<Result> AddReactionAsync(Guid conversationId, Guid messageId, AddReactionRequest request);

    /// <summary>
    /// Removes a reaction from a message
    /// DELETE /api/conversations/{conversationId}/messages/{messageId}/reactions
    /// </summary>
    Task<Result> RemoveReactionAsync(Guid conversationId, Guid messageId, RemoveReactionRequest request);
}
