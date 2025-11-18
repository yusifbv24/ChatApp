using ChatApp.Blazor.Client.Models.Channels;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Channels.Services;

/// <summary>
/// Interface for channel message operations
/// </summary>
public interface IChannelMessageService
{
    Task<Result<List<ChannelMessageDto>>> GetMessagesAsync(Guid channelId, int pageSize = 50, DateTime? before = null);
    Task<Result<List<ChannelMessageDto>>> GetPinnedMessagesAsync(Guid channelId);
    Task<Result<int>> GetUnreadCountAsync(Guid channelId);
    Task<Result<Guid>> SendMessageAsync(Guid channelId, SendMessageRequest request);
    Task<Result> EditMessageAsync(Guid channelId, Guid messageId, EditMessageRequest request);
    Task<Result> DeleteMessageAsync(Guid channelId, Guid messageId);
    Task<Result> PinMessageAsync(Guid channelId, Guid messageId);
    Task<Result> UnpinMessageAsync(Guid channelId, Guid messageId);
    Task<Result> AddReactionAsync(Guid channelId, Guid messageId, AddReactionRequest request);
    Task<Result> RemoveReactionAsync(Guid channelId, Guid messageId, RemoveReactionRequest request);
}
