using ChatApp.Modules.Notifications.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Notifications.Application.Queries.GetUnreadCount
{
    public record GetUnreadCountQuery(
        Guid UserId
    ):IRequest<Result<int>>;

    
    public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetUnreadCountQueryHandler> _logger;
        public GetUnreadCountQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetUnreadCountQueryHandler> logger)
        {
            _unitOfWork= unitOfWork;
            _logger= logger;
        }

        public async Task<Result<int>> Handle(
            GetUnreadCountQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var count = await _unitOfWork.Notifications.GetUnreadCountAsync(
                    request.UserId,
                    cancellationToken);

                return Result.Success(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread count for user {UserId}", request.UserId);
                return Result.Failure<int>("An error occurred while retrieving unread count");
            }
        }
    }
}