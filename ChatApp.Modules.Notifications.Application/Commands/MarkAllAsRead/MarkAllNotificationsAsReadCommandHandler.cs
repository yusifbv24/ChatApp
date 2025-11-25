using ChatApp.Modules.Notifications.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Notifications.Application.Commands.MarkAllAsRead
{
    public record MarkAllNotificationsAsReadCommand(
        Guid UserId
    ):IRequest<Result>;


    public class MarkAllNotificationsAsReadCommandValidator : AbstractValidator<MarkAllNotificationsAsReadCommand>
    {
        public MarkAllNotificationsAsReadCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }


    public class MarkAllNotificationsAsReadCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<MarkAllNotificationsAsReadCommandHandler> logger) : IRequestHandler<MarkAllNotificationsAsReadCommand, Result>
    {
        public async Task<Result> Handle(
            MarkAllNotificationsAsReadCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.Notifications.MarkAllAsReadAsync(request.UserId, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error marking all notifications as read");
                return Result.Failure("An error occurred while marking notifications as read");
            }
        }
    }
}