using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.DirectMessages;

namespace ChatApp.Blazor.Client.Features.DirectMessages.Services;

/// <summary>
/// Implementation of direct conversation service
/// Maps to: /api/conversations
/// </summary>
public class DirectConversationService : IDirectConversationService
{
    private readonly IApiClient _apiClient;

    public DirectConversationService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets all conversations for the current user
    /// GET /api/conversations
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result<List<DirectConversationDto>>> GetConversationsAsync()
    {
        return await _apiClient.GetAsync<List<DirectConversationDto>>("/api/conversations");
    }

    /// <summary>
    /// Starts a new conversation with another user
    /// POST /api/conversations
    /// Requires: Messages.Send permission
    /// Returns existing conversation if already exists (idempotent)
    /// </summary>
    public async Task<Result<Guid>> StartConversationAsync(StartConversationRequest request)
    {
        return await _apiClient.PostAsync<Guid>("/api/conversations", request);
    }
}
