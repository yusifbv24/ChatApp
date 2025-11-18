using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.DirectMessages;

namespace ChatApp.Blazor.Client.Features.DirectMessages.Services;

/// <summary>
/// Interface for direct conversation management
/// </summary>
public interface IDirectConversationService
{
    /// <summary>
    /// Gets all conversations for the current user
    /// GET /api/conversations
    /// </summary>
    Task<Result<List<DirectConversationDto>>> GetConversationsAsync();

    /// <summary>
    /// Starts a new conversation with another user
    /// POST /api/conversations
    /// </summary>
    Task<Result<Guid>> StartConversationAsync(StartConversationRequest request);
}
