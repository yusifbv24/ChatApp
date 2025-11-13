using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Channels;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Channels.Services;

/// <summary>
/// Implementation of channel member service
/// Maps to: /api/channels/{channelId}/members
/// </summary>
public class ChannelMemberService : IChannelMemberService
{
    private readonly IApiClient _apiClient;

    public ChannelMemberService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets channel members - GET /api/channels/{channelId}/members
    /// Requires: Groups.Read permission
    /// </summary>
    public async Task<Result<List<ChannelMemberDto>>> GetMembersAsync(Guid channelId)
    {
        return await _apiClient.GetAsync<List<ChannelMemberDto>>($"/api/channels/{channelId}/members");
    }

    /// <summary>
    /// Adds a member - POST /api/channels/{channelId}/members
    /// Requires: Groups.Manage permission
    /// </summary>
    public async Task<Result> AddMemberAsync(Guid channelId, AddMemberRequest request)
    {
        return await _apiClient.PostAsync($"/api/channels/{channelId}/members", request);
    }

    /// <summary>
    /// Removes a member - DELETE /api/channels/{channelId}/members/{userId}
    /// Requires: Groups.Manage permission
    /// </summary>
    public async Task<Result> RemoveMemberAsync(Guid channelId, Guid userId)
    {
        return await _apiClient.DeleteAsync($"/api/channels/{channelId}/members/{userId}");
    }

    /// <summary>
    /// Updates member role - PUT /api/channels/{channelId}/members/{userId}/role
    /// Requires: Groups.Manage permission
    /// </summary>
    public async Task<Result> UpdateMemberRoleAsync(Guid channelId, Guid userId, UpdateMemberRoleRequest request)
    {
        return await _apiClient.PutAsync($"/api/channels/{channelId}/members/{userId}/role", request);
    }

    /// <summary>
    /// Leave channel - POST /api/channels/{channelId}/members/leave
    /// Requires: Groups.Manage permission
    /// </summary>
    public async Task<Result> LeaveChannelAsync(Guid channelId)
    {
        return await _apiClient.PostAsync($"/api/channels/{channelId}/members/leave");
    }
}
