using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Services;

/// <summary>
/// Service interface for direct message conversations
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Gets all conversations for the current user
    /// </summary>
    Task<Result<List<DirectConversationDto>>> GetConversationsAsync();

    /// <summary>
    /// Starts a new conversation with another user
    /// </summary>
    Task<Result<Guid>> StartConversationAsync(Guid otherUserId);

    /// <summary>
    /// Gets messages in a conversation with pagination
    /// </summary>
    Task<Result<List<DirectMessageDto>>> GetMessagesAsync(Guid conversationId, int pageSize = 50, DateTime? before = null);

    /// <summary>
    /// Gets unread message count for a conversation
    /// </summary>
    Task<Result<int>> GetUnreadCountAsync(Guid conversationId);

    /// <summary>
    /// Sends a message in a conversation
    /// </summary>
    Task<Result<Guid>> SendMessageAsync(Guid conversationId, string content, string? fileId = null);

    /// <summary>
    /// Edits a message
    /// </summary>
    Task<Result> EditMessageAsync(Guid conversationId, Guid messageId, string newContent);

    /// <summary>
    /// Deletes a message
    /// </summary>
    Task<Result> DeleteMessageAsync(Guid conversationId, Guid messageId);

    /// <summary>
    /// Marks a message as read
    /// </summary>
    Task<Result> MarkAsReadAsync(Guid conversationId, Guid messageId);

    /// <summary>
    /// Adds a reaction to a message
    /// </summary>
    Task<Result> AddReactionAsync(Guid conversationId, Guid messageId, string reaction);

    /// <summary>
    /// Removes a reaction from a message
    /// </summary>
    Task<Result> RemoveReactionAsync(Guid conversationId, Guid messageId, string reaction);
}
