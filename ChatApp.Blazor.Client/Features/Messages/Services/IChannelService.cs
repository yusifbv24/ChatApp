using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Services;

/// <summary>
/// Service interface for channel operations
/// </summary>
public interface IChannelService
{
    /// <summary>
    /// Gets all channels the current user is a member of
    /// </summary>
    Task<Result<List<ChannelDto>>> GetMyChannelsAsync();

    /// <summary>
    /// Gets all public channels
    /// </summary>
    Task<Result<List<ChannelDto>>> GetPublicChannelsAsync();

    /// <summary>
    /// Gets detailed channel information
    /// </summary>
    Task<Result<ChannelDetailsDto>> GetChannelAsync(Guid channelId);

    /// <summary>
    /// Searches channels by name or description
    /// </summary>
    Task<Result<List<ChannelDto>>> SearchChannelsAsync(string query);

    /// <summary>
    /// Creates a new channel
    /// </summary>
    Task<Result<Guid>> CreateChannelAsync(CreateChannelRequest request);

    /// <summary>
    /// Updates a channel
    /// </summary>
    Task<Result> UpdateChannelAsync(Guid channelId, UpdateChannelRequest request);

    /// <summary>
    /// Deletes (archives) a channel
    /// </summary>
    Task<Result> DeleteChannelAsync(Guid channelId);

    /// <summary>
    /// Gets messages in a channel with pagination
    /// </summary>
    Task<Result<List<ChannelMessageDto>>> GetMessagesAsync(Guid channelId, int pageSize = 50, DateTime? before = null);

    /// <summary>
    /// Gets pinned messages in a channel
    /// </summary>
    Task<Result<List<ChannelMessageDto>>> GetPinnedMessagesAsync(Guid channelId);

    /// <summary>
    /// Gets unread message count for a channel
    /// </summary>
    Task<Result<int>> GetUnreadCountAsync(Guid channelId);

    /// <summary>
    /// Sends a message to a channel
    /// </summary>
    Task<Result<Guid>> SendMessageAsync(Guid channelId, string content, string? fileId = null);

    /// <summary>
    /// Edits a message
    /// </summary>
    Task<Result> EditMessageAsync(Guid channelId, Guid messageId, string newContent);

    /// <summary>
    /// Deletes a message
    /// </summary>
    Task<Result> DeleteMessageAsync(Guid channelId, Guid messageId);

    /// <summary>
    /// Pins a message
    /// </summary>
    Task<Result> PinMessageAsync(Guid channelId, Guid messageId);

    /// <summary>
    /// Unpins a message
    /// </summary>
    Task<Result> UnpinMessageAsync(Guid channelId, Guid messageId);

    /// <summary>
    /// Adds a reaction to a message
    /// </summary>
    Task<Result> AddReactionAsync(Guid channelId, Guid messageId, string reaction);

    /// <summary>
    /// Removes a reaction from a message
    /// </summary>
    Task<Result> RemoveReactionAsync(Guid channelId, Guid messageId, string reaction);

    /// <summary>
    /// Joins a channel (for public channels)
    /// </summary>
    Task<Result> JoinChannelAsync(Guid channelId);

    /// <summary>
    /// Leaves a channel
    /// </summary>
    Task<Result> LeaveChannelAsync(Guid channelId);
}
