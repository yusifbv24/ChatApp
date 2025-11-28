using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;

namespace ChatApp.Blazor.Client.Features.Messages.Services
{
    public class ChannelService(IApiClient apiClient) : IChannelService
    {
        public async Task<Result<List<ChannelDto>>> GetMyChannelsAsync()
        {
            return await apiClient.GetAsync<List<ChannelDto>>("/api/channels/my-channels");
        }


        public async Task<Result<ChannelDetailsDto>> GetChannelAsync(Guid channelId)
        {
            return await apiClient.GetAsync<ChannelDetailsDto>($"/api/channels/{channelId}");
        }


        public async Task<Result<List<ChannelDto>>> GetPublicChannelsAsync()
        {
            return await apiClient.GetAsync<List<ChannelDto>>("/api/channels/public");
        }


        public async Task<Result<List<ChannelDto>>> SearchChannelsAsync(string query)
        {
            return await apiClient.GetAsync<List<ChannelDto>>(
                $"/api/channels/search?query={Uri.EscapeDataString(query)}");
        }


        public async Task<Result<Guid>> CreateChannelAsync(CreateChannelRequest request)
        {
            var response = await apiClient.PostAsync<CreateChannelResponse>("/api/channels", request);

            if(response.IsSuccess && response.Value != null)
            {
                return Result.Success(response.Value.ChannelId);
            }

            return Result.Failure<Guid>(response.Error ?? "Failed to create channel");
        }


        public async Task<Result> UpdateChannelAsync(Guid channelId, UpdateChannelRequest request)
        {
            return await apiClient.PutAsync($"/api/channels/{channelId}", request);
        }


        public async Task<Result> DeleteChannelAsync(Guid channelId)
        {
            return await apiClient.DeleteAsync($"/api/channels/{channelId}");
        }


        public async Task<Result<List<ChannelMessageDto>>> GetMessagesAsync(
            Guid channelId, 
            int pageSize = 50, 
            DateTime? before = null)
        {
            var url = $"/api/channels/{channelId}/messages?pageSize={pageSize}";
            if (before.HasValue)
            {
                url+=$"&before={before.Value}";
            }

            return await apiClient.GetAsync<List<ChannelMessageDto>>(url);
        }


        public async Task<Result<List<ChannelMessageDto>>> GetPinnedMessagesAsync(Guid channelId)
        {
            return await apiClient.GetAsync<List<ChannelMessageDto>>(
                $"/api/channels/{channelId}/messages/pinned");
        }


        public async Task<Result<int>> GetUnreadCountAsync(Guid channelId)
        {
            var response = await apiClient.GetAsync<UnreadCountResponse>(
                $"/api/channels/{channelId}/messages/unread-count");

            if(response.IsSuccess && response.Value != null)
            {
                return Result.Success(response.Value.UnreadCount);
            }

            return Result.Failure<int>(response.Error ?? "Failed to get unread count");
        }



        public async Task<Result<Guid>> SendMessageAsync(Guid channelId, string content, string? fileId = null)
        {
            var response = await apiClient.PostAsync<SendMessageResponse>(
                $"/api/channels/{channelId}/messages",
                new { Content = content, FileId = fileId });

            if(response.IsSuccess && response.Value != null)
            {
                return Result.Success(response.Value.MessageId);
            }

            return Result.Failure<Guid>(response.Error ?? "Failed to send message");
        }



        public async Task<Result> EditMessageAsync(Guid channelId, Guid messageId, string newContent)
        {
            return await apiClient.PutAsync(
                $"/api/channels/{channelId}/messages/{messageId}",
                new {Content=newContent});
        }



        public async Task<Result> DeleteMessageAsync(Guid channelId, Guid messageId)
        {
            return await apiClient.DeleteAsync(
                $"/api/channels/{channelId}/messages/{messageId}");
        }



        public async Task<Result> PinMessageAsync(Guid channelId, Guid messageId)
        {
            return await apiClient.PostAsync(
                $"/api/channels/{channelId}/messages/{messageId}/pin");
        }


        public async Task<Result> UnPinMessageAsync(Guid channelId, Guid messageId)
        {
            return await apiClient.DeleteAsync(
                $"/api/channels/{channelId}/messages/{messageId}/pin");
        }


        public async Task<Result> AddReactionAsync(Guid channelId, Guid messageId, string reaction)
        {
            return await apiClient.PostAsync(
                $"/api/channels/{channelId}/messages/{messageId}/reactions",
                new {Reaction=reaction});
        }



        public async Task<Result> RemoveReactionAsync(Guid channelId, Guid messageId, string reaction)
        {
            return await apiClient.DeleteAsync(
                $"/api/channels/{channelId}/messages/{messageId}/reactions");
        }



        public async Task<Result> JoinChannelAsync(Guid channelId)
        {
            return await apiClient.PostAsync($"/api/channels/{channelId}/members/join");
        }


        public async Task<Result> LeaveChannelAsync(Guid channelId)
        {
            return await apiClient.PostAsync($"/api/channels/{channelId}/members/leave");
        }


        private record CreateChannelResponse(Guid ChannelId,string Message);

        private record SendMessageResponse(Guid MessageId,string Message);

        private record UnreadCountResponse(int UnreadCount);
    }
}