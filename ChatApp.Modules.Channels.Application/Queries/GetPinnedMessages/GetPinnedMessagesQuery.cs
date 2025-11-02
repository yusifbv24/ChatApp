using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Queries.GetPinnedMessages
{
    public record GetPinnedMessagesQuery(
        Guid ChannelId,
        Guid RequestedBy
    ) : IRequest<Result<List<ChannelMessageDto>>>;

    public class GetPinnedMessagesQueryHandler : IRequestHandler<GetPinnedMessagesQuery, Result<List<ChannelMessageDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetPinnedMessagesQueryHandler> _logger;

        public GetPinnedMessagesQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetPinnedMessagesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<ChannelMessageDto>>> Handle(
            GetPinnedMessagesQuery request,
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
                    return Result.Failure<List<ChannelMessageDto>>("You must be a member to view pinned messages");
                }

                // Repository handles the database join and returns DTOs
                var messageDtos = await _unitOfWork.ChannelMessages.GetPinnedMessagesAsync(
                    request.ChannelId,
                    cancellationToken);

                return Result.Success(messageDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pinned messages for channel {ChannelId}", request.ChannelId);
                return Result.Failure<List<ChannelMessageDto>>("An error occurred while retrieving pinned messages");
            }
        }
    }
}