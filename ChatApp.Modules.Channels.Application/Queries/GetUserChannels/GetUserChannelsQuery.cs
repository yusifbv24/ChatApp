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
                var channels = await _unitOfWork.Channels.GetUserChannelsAsync(
                    request.UserId,
                    cancellationToken);

                var channelDtos = channels
                    .Where(c => !c.IsArchived)
                    .Select(c => new ChannelDto(
                        c.Id,
                        c.Name,
                        c.Description,
                        c.Type,
                        c.CreatedBy,
                        c.Members.Count(m => m.IsActive),
                        c.IsArchived,
                        c.CreatedAtUtc,
                        c.ArchivedAtUtc
                    ))
                    .OrderByDescending(c => c.CreatedAtUtc)
                    .ToList();

                return Result.Success(channelDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving channels for user {UserId}", request.UserId);
                return Result.Failure<List<ChannelDto>>("An error occurred while retrieving channels");
            }
        }
    }
}