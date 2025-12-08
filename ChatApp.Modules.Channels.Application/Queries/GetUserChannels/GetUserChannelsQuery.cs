using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Queries.GetUserChannels
{
    public record GetUserChannelsQuery(
        Guid UserId
    ) : IRequest<Result<List<ChannelDto>>>;

    public class GetUserChannelsQueryHandler : IRequestHandler<GetUserChannelsQuery, Result<List<ChannelDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetUserChannelsQueryHandler> _logger;

        public GetUserChannelsQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetUserChannelsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<ChannelDto>>> Handle(
            GetUserChannelsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Use the new method that includes last message info
                var channelDtos = await _unitOfWork.Channels.GetUserChannelDtosAsync(
                    request.UserId,
                    cancellationToken);

                return Result.Success(channelDtos);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving channels for user {UserId}", request.UserId);
                return Result.Failure<List<ChannelDto>>("An error occurred while retrieving channels");
            }
        }
    }
}