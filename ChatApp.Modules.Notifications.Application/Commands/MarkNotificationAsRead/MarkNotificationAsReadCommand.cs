using ChatApp.Modules.Notifications.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Notifications.Application.Commands.MarkNotificationAsRead
{
    public record MarkNotificationAsReadCommand(
        Guid NotificationId,
        Guid UserId
    ):IRequest<Result>;


    
    public class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
    {
        public MarkNotificationAsReadCommandValidator()
        {
            RuleFor(x => x.NotificationId)
                .NotEmpty().WithMessage("Notification ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }


    public class MarkNotificationAsReadCommandHandler(
        IUnitOfWork unitOfWork) : IRequestHandler<MarkNotificationAsReadCommand, Result>
    {
        public async Task<Result> Handle(
            MarkNotificationAsReadCommand request,
            CancellationToken cancellationToken)
        {
            var notification = await unitOfWork.Notifications.GetByIdAsync(
                request.NotificationId,
                cancellationToken) 
                    ?? throw new NotFoundException($"Notification with ID {request.NotificationId} not found");

            if (notification.UserId != request.UserId)
            {
                return Result.Failure("You can only mark your own notification as read");
            }
            try
            {
                notification.MarkAsRead();

                await unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                notification.MarkAsFailed("Error marking notification as read");
                return Result.Failure(ex.Message);
            }
        }
    }
}