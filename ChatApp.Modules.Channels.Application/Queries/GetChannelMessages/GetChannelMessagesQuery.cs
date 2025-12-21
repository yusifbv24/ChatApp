using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Queries.GetChannelMessages
{
    public record GetChannelMessagesQuery(
        Guid ChannelId,
        Guid RequestedBy,
        int PageSize = 50,
        DateTime? BeforeUtc = null
    ) : IRequest<Result<List<ChannelMessageDto>>>;

    public class GetChannelMessagesQueryHandler : IRequestHandler<GetChannelMessagesQuery, Result<List<ChannelMessageDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetChannelMessagesQueryHandler> _logger;
        private readonly IChannelMemberCache _channelMemberCache;

        public GetChannelMessagesQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetChannelMessagesQueryHandler> logger,
            IChannelMemberCache channelMemberCache)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _channelMemberCache = channelMemberCache;
        }

        public async Task<Result<List<ChannelMessageDto>>> Handle(
            GetChannelMessagesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var channel = await _unitOfWork.Channels.GetByIdAsync(
                    request.ChannelId,
                    cancellationToken);

                if (channel == null)
                {
                    return Result.Failure<List<ChannelMessageDto>>("Channel not found");
                }

                // Verify user is a member
                var isMember = await _unitOfWork.Channels.IsUserMemberAsync(
                    request.ChannelId,
                    request.RequestedBy,
                    cancellationToken);

                if (!isMember)
                {
                    return Result.Failure<List<ChannelMessageDto>>("You must be a member to view channel messages");
                }

                // Get messages (repository will handle pagination and joining with user data)
                var messages = await _unitOfWork.ChannelMessages.GetChannelMessagesAsync(
                    request.ChannelId,
                    request.PageSize,
                    request.BeforeUtc,
                    cancellationToken);

                // Populate channel member cache for typing indicators
                // This ensures typing works even on first visit (before any message is sent)
                var memberIds = await _unitOfWork.Channels.GetMemberUserIdsAsync(
                    request.ChannelId,
                    cancellationToken);
                await _channelMemberCache.UpdateChannelMembersAsync(request.ChannelId, memberIds);

                return Result.Success(messages);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving messages for channel {ChannelId}", request.ChannelId);
                return Result.Failure<List<ChannelMessageDto>>("An error occurred while retrieving messages");
            }
        }
    }
}