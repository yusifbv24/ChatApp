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


    public class GetUserNotificationsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUserNotificationsQueryHandler> logger) : IRequestHandler<GetUserNotificationsQuery, Result<List<NotificationDto>>>
    {
        public async Task<Result<List<NotificationDto>>> Handle(
            GetUserNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var notifications = await unitOfWork.Notifications.GetUserNotificationsAsync(
                    request.UserId,
                    request.PageSize,
                    request.skip,
                    cancellationToken);
                
                return Result.Success(notifications);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving notifications for user {UserId}", request.UserId);
                return Result.Failure<List<NotificationDto>>("An error occurred while retrieving notifications");
            }
        }
    }
}