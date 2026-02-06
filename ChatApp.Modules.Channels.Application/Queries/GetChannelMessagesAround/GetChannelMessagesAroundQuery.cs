using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Queries.GetChannelMessagesAround
{
    public record GetChannelMessagesAroundQuery(
        Guid ChannelId,
        Guid MessageId,
        Guid RequestedBy,
        int Count = 50
    ) : IRequest<Result<List<ChannelMessageDto>>>;

    public class GetChannelMessagesAroundQueryHandler : IRequestHandler<GetChannelMessagesAroundQuery, Result<List<ChannelMessageDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetChannelMessagesAroundQueryHandler> _logger;

        public GetChannelMessagesAroundQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetChannelMessagesAroundQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<ChannelMessageDto>>> Handle(
            GetChannelMessagesAroundQuery request,
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

                // Public channel-da mesajları hər kəs görə bilər
                // Private channel-da yalnız üzvlər görə bilər
                if (channel.Type == Domain.Enums.ChannelType.Private)
                {
                    var isMember = await _unitOfWork.Channels.IsUserMemberAsync(
                        request.ChannelId,
                        request.RequestedBy,
                        cancellationToken);

                    if (!isMember)
                    {
                        return Result.Failure<List<ChannelMessageDto>>("You must be a member to view private channel messages");
                    }
                }

                // Get messages around the target message
                var messages = await _unitOfWork.ChannelMessages.GetMessagesAroundAsync(
                    request.ChannelId,
                    request.MessageId,
                    request.Count,
                    cancellationToken);

                if (messages.Count == 0)
                {
                    return Result.Failure<List<ChannelMessageDto>>("Message not found");
                }

                return Result.Success(messages);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving messages around {MessageId} for channel {ChannelId}",
                    request.MessageId, request.ChannelId);
                return Result.Failure<List<ChannelMessageDto>>("An error occurred while retrieving messages");
            }
        }
    }
}
