using ChatApp.Blazor.Client.Models.Channels;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Channels.Services;

/// <summary>
/// Interface for channel management operations
/// </summary>
public interface IChannelService
{
    Task<Result<Guid>> CreateChannelAsync(CreateChannelRequest request);
    Task<Result<ChannelDetailsDto>> GetChannelAsync(Guid channelId);
    Task<Result<List<ChannelDto>>> GetMyChannelsAsync();
    Task<Result<List<ChannelDto>>> GetPublicChannelsAsync();
    Task<Result<List<ChannelDto>>> SearchChannelsAsync(string query);
    Task<Result> UpdateChannelAsync(Guid channelId, UpdateChannelRequest request);
    Task<Result> DeleteChannelAsync(Guid channelId);
}
