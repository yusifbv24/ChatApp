using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Channels;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Channels.Services;

/// <summary>
/// Implementation of channel management service
/// Maps to: /api/channels
/// </summary>
public class ChannelService : IChannelService
{
    private readonly IApiClient _apiClient;

    public ChannelService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Creates a new channel - POST /api/channels
    /// Requires: Groups.Create permission
    /// </summary>
    public async Task<Result<Guid>> CreateChannelAsync(CreateChannelRequest request)
    {
        return await _apiClient.PostAsync<Guid>("/api/channels", request);
    }

    /// <summary>
    /// Gets channel details - GET /api/channels/{channelId}
    /// Requires: Groups.Read permission
    /// </summary>
    public async Task<Result<ChannelDetailsDto>> GetChannelAsync(Guid channelId)
    {
        return await _apiClient.GetAsync<ChannelDetailsDto>($"/api/channels/{channelId}");
    }

    /// <summary>
    /// Gets user's channels - GET /api/channels/my-channels
    /// Requires: Groups.Read permission
    /// </summary>
    public async Task<Result<List<ChannelDto>>> GetMyChannelsAsync()
    {
        return await _apiClient.GetAsync<List<ChannelDto>>("/api/channels/my-channels");
    }

    /// <summary>
    /// Gets public channels - GET /api/channels/public
    /// Requires: Groups.Read permission
    /// </summary>
    public async Task<Result<List<ChannelDto>>> GetPublicChannelsAsync()
    {
        return await _apiClient.GetAsync<List<ChannelDto>>("/api/channels/public");
    }

    /// <summary>
    /// Searches channels - GET /api/channels/search?query={query}
    /// Requires: Groups.Read permission
    /// </summary>
    public async Task<Result<List<ChannelDto>>> SearchChannelsAsync(string query)
    {
        return await _apiClient.GetAsync<List<ChannelDto>>($"/api/channels/search?query={Uri.EscapeDataString(query)}");
    }

    /// <summary>
    /// Updates channel - PUT /api/channels/{channelId}
    /// Requires: Groups.Manage permission
    /// </summary>
    public async Task<Result> UpdateChannelAsync(Guid channelId, UpdateChannelRequest request)
    {
        return await _apiClient.PutAsync($"/api/channels/{channelId}", request);
    }

    /// <summary>
    /// Deletes (archives) channel - DELETE /api/channels/{channelId}
    /// Requires: Groups.Manage permission
    /// </summary>
    public async Task<Result> DeleteChannelAsync(Guid channelId)
    {
        return await _apiClient.DeleteAsync($"/api/channels/{channelId}");
    }
}
