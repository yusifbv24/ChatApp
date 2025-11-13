using ChatApp.Blazor.Client.Models.Channels;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Channels.Services;

/// <summary>
/// Interface for channel member operations
/// </summary>
public interface IChannelMemberService
{
    Task<Result<List<ChannelMemberDto>>> GetMembersAsync(Guid channelId);
    Task<Result> AddMemberAsync(Guid channelId, AddMemberRequest request);
    Task<Result> RemoveMemberAsync(Guid channelId, Guid userId);
    Task<Result> UpdateMemberRoleAsync(Guid channelId, Guid userId, UpdateMemberRoleRequest request);
    Task<Result> LeaveChannelAsync(Guid channelId);
}
