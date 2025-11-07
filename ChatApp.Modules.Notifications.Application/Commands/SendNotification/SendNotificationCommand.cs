using ChatApp.Modules.Notifications.Application.Interfaces;
using ChatApp.Modules.Notifications.Domain.Entities;
using ChatApp.Modules.Notifications.Domain.Enums;
using ChatApp.Modules.Notifications.Domain.Events;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Notifications.Application.Commands.SendNotification
{
    public record SendNotificationCommand(
        Guid UserId,
        NotificationType Type,
        NotificationChannel Channel,
        string Title,
        string Message,
        string? ActionUrl=null,
        Guid? SourceId=null,
        Guid? SenderId=null
    ):IRequest<Result<Guid>>;


    public class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
    {
        public SendNotificationCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message is required")
                .MaximumLength(1000).WithMessage("Message cannot exceed 1000 characters");
        }
    }


    public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;
        private readonly ILogger<SendNotificationCommandHandler> _logger;
        public SendNotificationCommandHandler(
            IUnitOfWork unitOfWork,
            IEventBus eventBus,
            ILogger<SendNotificationCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _eventBus = eventBus;
            _logger = logger;
        }


        public async Task<Result<Guid>> Handle(
            SendNotificationCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var notification = new Notification(
                    request.UserId,
                    request.Type,
                    request.Channel,
                    request.Title,
                    request.Message,
                    request.ActionUrl,
                    request.SourceId,
                    request.SenderId);

                notification.MarkAsSent();

                await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _eventBus.PublishAsync(
                    new NotificationSentEvent(
                        notification.Id,
                        notification.UserId,
                        notification.Type,
                        notification.Channel,
                        notification.CreatedAtUtc)
                    ,cancellationToken);

                _logger?.LogInformation("Notification {NotificationId} created", notification.Id);

                return Result.Success(notification.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating notification");
                return Result.Failure<Guid>("An error occured while creating the notification");
            }
        }    
    }
}