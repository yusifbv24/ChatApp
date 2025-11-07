using ChatApp.Modules.Notifications.Application.DTOs;
using ChatApp.Modules.Notifications.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Notifications.Application.Queries.GetUserNotifications
{
    public record GetUserNotificationsQuery(
        Guid UserId,
        int PageSize=50,
        int skip=0
    ):IRequest<Result<List<NotificationDto>>>;


    public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, Result<List<NotificationDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetUserNotificationsQueryHandler> _logger;
        public GetUserNotificationsQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetUserNotificationsQueryHandler> logger)
        {
            _unitOfWork=unitOfWork;
            _logger=logger;
        }


        public async Task<Result<List<NotificationDto>>> Handle(
            GetUserNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var notifications = await _unitOfWork.Notifications.GetUserNotificationsAsync(
                    request.UserId,
                    request.PageSize,
                    request.skip,
                    cancellationToken);
                
                return Result.Success(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user {UserId}", request.UserId);
                return Result.Failure<List<NotificationDto>>("An error occurred while retrieving notifications");
            }
        }
    }
}