using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Queries.GetChannel
{
    public record GetChannelQuery(
        Guid ChannelId,
        Guid RequestedBy
    ) : IRequest<Result<ChannelDetailsDto?>>;

    public class GetChannelQueryHandler : IRequestHandler<GetChannelQuery, Result<ChannelDetailsDto?>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetChannelQueryHandler> _logger;

        public GetChannelQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetChannelQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<ChannelDetailsDto?>> Handle(
            GetChannelQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Simple check for existence
                var channel = await _unitOfWork.Channels.GetByIdAsync(
                    request.ChannelId,
                    cancellationToken);

                if (channel == null)
                    return Result.Success<ChannelDetailsDto?>(null);

                // For private channels, verify user is a member
                if (channel.Type == Domain.Enums.ChannelType.Private)
                {
                    var isMember = await _unitOfWork.Channels.IsUserMemberAsync(
                        request.ChannelId,
                        request.RequestedBy,
                        cancellationToken);

                    if (!isMember)
                    {
                        return Result.Failure<ChannelDetailsDto?>("You don't have access to this private channel");
                    }
                }

                // Get full details with user data - all joins happen in repository
                var channelDetails = await _unitOfWork.Channels.GetChannelDetailsByIdAsync(
                    request.ChannelId,
                    cancellationToken);

                return Result.Success(channelDetails);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving channel {ChannelId}", request.ChannelId);
                return Result.Failure<ChannelDetailsDto?>("An error occurred while retrieving the channel");
            }
        }
    }
}