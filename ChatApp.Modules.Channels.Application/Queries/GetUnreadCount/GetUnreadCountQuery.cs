using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Queries.GetUnreadCount
{
    public record GetUnreadCountQuery(
        Guid ChannelId,
        Guid UserId
    ) : IRequest<Result<int>>;

    public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetUnreadCountQueryHandler> _logger;

        public GetUnreadCountQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetUnreadCountQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<int>> Handle(
            GetUnreadCountQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Verify user is a member
                var isMember = await _unitOfWork.Channels.IsUserMemberAsync(
                    request.ChannelId,
                    request.UserId,
                    cancellationToken);

                if (!isMember)
                {
                    return Result.Failure<int>("You must be a member to view unread count");
                }

                var unreadCount = await _unitOfWork.ChannelMessages.GetUnreadCountAsync(
                    request.ChannelId,
                    request.UserId,
                    cancellationToken);

                return Result.Success(unreadCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error retrieving unread count for channel {ChannelId} and user {UserId}",
                    request.ChannelId,
                    request.UserId);
                return Result.Failure<int>("An error occurred while retrieving unread count");
            }
        }
    }
}