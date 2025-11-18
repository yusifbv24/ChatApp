using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Channels;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Channels.Services;

/// <summary>
/// Implementation of channel message service
/// Maps to: /api/channels/{channelId}/messages
/// </summary>
public class ChannelMessageService : IChannelMessageService
{
    private readonly IApiClient _apiClient;

    public ChannelMessageService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets messages with pagination - GET /api/channels/{channelId}/messages
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result<List<ChannelMessageDto>>> GetMessagesAsync(Guid channelId, int pageSize = 50, DateTime? before = null)
    {
        var beforeParam = before.HasValue ? $"&before={before.Value:O}" : "";
        return await _apiClient.GetAsync<List<ChannelMessageDto>>(
            $"/api/channels/{channelId}/messages?pageSize={pageSize}{beforeParam}");
    }

    /// <summary>
    /// Gets pinned messages - GET /api/channels/{channelId}/messages/pinned
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result<List<ChannelMessageDto>>> GetPinnedMessagesAsync(Guid channelId)
    {
        return await _apiClient.GetAsync<List<ChannelMessageDto>>($"/api/channels/{channelId}/messages/pinned");
    }

    /// <summary>
    /// Gets unread count - GET /api/channels/{channelId}/messages/unread-count
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result<int>> GetUnreadCountAsync(Guid channelId)
    {
        var result = await _apiClient.GetAsync<dynamic>($"/api/channels/{channelId}/messages/unread-count");
        if (result.IsSuccess && result.Value != null)
        {
            var unreadCount = (int)result.Value.unreadCount;
            return Result.Success(unreadCount);
        }
        return Result.Failure<int>(result.Error ?? "Failed to get unread count");
    }

    /// <summary>
    /// Sends a message - POST /api/channels/{channelId}/messages
    /// Requires: Messages.Send permission
    /// </summary>
    public async Task<Result<Guid>> SendMessageAsync(Guid channelId, SendMessageRequest request)
    {
        return await _apiClient.PostAsync<Guid>($"/api/channels/{channelId}/messages", request);
    }

    /// <summary>
    /// Edits a message - PUT /api/channels/{channelId}/messages/{messageId}
    /// Requires: Messages.Edit permission
    /// </summary>
    public async Task<Result> EditMessageAsync(Guid channelId, Guid messageId, EditMessageRequest request)
    {
        return await _apiClient.PutAsync($"/api/channels/{channelId}/messages/{messageId}", request);
    }

    /// <summary>
    /// Deletes a message - DELETE /api/channels/{channelId}/messages/{messageId}
    /// Requires: Messages.Delete permission
    /// </summary>
    public async Task<Result> DeleteMessageAsync(Guid channelId, Guid messageId)
    {
        return await _apiClient.DeleteAsync($"/api/channels/{channelId}/messages/{messageId}");
    }

    /// <summary>
    /// Pins a message - POST /api/channels/{channelId}/messages/{messageId}/pin
    /// Requires: Groups.Manage permission
    /// </summary>
    public async Task<Result> PinMessageAsync(Guid channelId, Guid messageId)
    {
        return await _apiClient.PostAsync($"/api/channels/{channelId}/messages/{messageId}/pin");
    }

    /// <summary>
    /// Unpins a message - DELETE /api/channels/{channelId}/messages/{messageId}/pin
    /// Requires: Groups.Manage permission
    /// </summary>
    public async Task<Result> UnpinMessageAsync(Guid channelId, Guid messageId)
    {
        return await _apiClient.DeleteAsync($"/api/channels/{channelId}/messages/{messageId}/pin");
    }

    /// <summary>
    /// Adds a reaction - POST /api/channels/{channelId}/messages/{messageId}/reactions
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result> AddReactionAsync(Guid channelId, Guid messageId, AddReactionRequest request)
    {
        return await _apiClient.PostAsync($"/api/channels/{channelId}/messages/{messageId}/reactions", request);
    }

    /// <summary>
    /// Removes a reaction - DELETE /api/channels/{channelId}/messages/{messageId}/reactions
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result> RemoveReactionAsync(Guid channelId, Guid messageId, RemoveReactionRequest request)
    {
        return await _apiClient.DeleteAsync($"/api/channels/{channelId}/messages/{messageId}/reactions");
    }
}
