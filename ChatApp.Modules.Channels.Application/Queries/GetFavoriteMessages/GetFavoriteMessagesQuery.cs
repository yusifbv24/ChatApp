using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Queries.GetFavoriteMessages
{
    public record GetFavoriteMessagesQuery(
        Guid ChannelId,
        Guid RequestedBy
    ) : IRequest<Result<List<FavoriteChannelMessageDto>>>;

    public class GetFavoriteMessagesQueryHandler : IRequestHandler<GetFavoriteMessagesQuery, Result<List<FavoriteChannelMessageDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetFavoriteMessagesQueryHandler> _logger;

        public GetFavoriteMessagesQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetFavoriteMessagesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<FavoriteChannelMessageDto>>> Handle(
            GetFavoriteMessagesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var channel = await _unitOfWork.Channels.GetByIdAsync(
                    request.ChannelId,
                    cancellationToken);

                if (channel == null)
                {
                    return Result.Failure<List<FavoriteChannelMessageDto>>("Channel not found");
                }

                // Verify user is a member
                var isMember = await _unitOfWork.Channels.IsUserMemberAsync(
                    request.ChannelId,
                    request.RequestedBy,
                    cancellationToken);

                if (!isMember)
                {
                    return Result.Failure<List<FavoriteChannelMessageDto>>("You must be a member to view favorite messages");
                }

                var favoriteMessages = await _unitOfWork.Favorites.GetFavoriteMessagesAsync(
                    request.RequestedBy,
                    request.ChannelId,
                    cancellationToken);

                return Result.Success(favoriteMessages);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving favorite messages for channel {ChannelId}", request.ChannelId);
                return Result.Failure<List<FavoriteChannelMessageDto>>("An error occurred while retrieving favorite messages");
            }
        }
    }
}
