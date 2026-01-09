using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Services
{
    public interface IChannelService
    {
        Task<Result<List<ChannelDto>>> GetMyChannelsAsync();

        
        Task<Result<List<ChannelDto>>> GetPublicChannelsAsync();


        Task<Result<ChannelDetailsDto>> GetChannelAsync(Guid channelId);


        Task<Result<List<ChannelDto>>> SearchChannelsAsync(string query);


        Task<Result<Guid>> CreateChannelAsync(CreateChannelRequest request);


        Task<Result> UpdateChannelAsync(Guid channelId, UpdateChannelRequest request);


        Task<Result> DeleteChannelAsync(Guid channelId);


        Task<Result<List<ChannelMessageDto>>> GetMessagesAsync(Guid channelId, int pageSize = 30, DateTime? before = null);


        Task<Result<List<ChannelMessageDto>>> GetMessagesAroundAsync(Guid channelId, Guid messageId, int count = 30);


        Task<Result<List<ChannelMessageDto>>> GetMessagesBeforeAsync(Guid channelId, DateTime beforeUtc, int limit = 100);


        Task<Result<List<ChannelMessageDto>>> GetMessagesAfterAsync(Guid channelId, DateTime afterUtc, int limit = 100);


        Task<Result<List<ChannelMessageDto>>> GetPinnedMessagesAsync(Guid channelId);


        Task<Result<List<FavoriteChannelMessageDto>>> GetFavoriteMessagesAsync(Guid channelId);


        Task<Result<int>> GetUnreadCountAsync(Guid channelId);


        Task<Result> MarkAsReadAsync(Guid channelId);


        Task<Result> MarkSingleMessageAsReadAsync(Guid channelId, Guid messageId);


        Task<Result<Guid>> SendMessageAsync(Guid channelId, string content, string? fileId = null, Guid? replyToMessageId = null, bool isForwarded = false);


        Task<Result> EditMessageAsync(Guid channelId, Guid messageId, string newContent);


        Task<Result> DeleteMessageAsync(Guid channelId, Guid messageId);


        Task<Result> PinMessageAsync(Guid channelId, Guid messageId);


        Task<Result> UnPinMessageAsync(Guid channelId, Guid messageId);


        Task<Result> ToggleMessageAsLaterAsync(Guid channelId, Guid messageId);


        Task<Result<List<ChannelMessageReactionDto>>> ToggleReactionAsync(Guid channelId, Guid messageId, string reaction);

        Task<Result> JoinChannelAsync(Guid channelId);


        Task<Result> LeaveChannelAsync(Guid channelId);


        Task<Result> AddMemberAsync(Guid channelId, Guid userId);


        Task<Result> UpdateMemberRoleAsync(Guid channelId, Guid userId, ChannelMemberRole newRole);


        Task<Result> RemoveMemberAsync(Guid channelId, Guid userId);


        Task<Result<bool>> ToggleFavoriteAsync(Guid channelId, Guid messageId);
    }
}