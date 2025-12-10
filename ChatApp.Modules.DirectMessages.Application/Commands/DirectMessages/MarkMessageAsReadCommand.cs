using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Events;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectMessages
{
    public record MarkMessageAsReadCommand(
        Guid MessageId,
        Guid UserId
    ):IRequest<Result>;


    public class MarkMessageAsReadCommandValidator : AbstractValidator<MarkMessageAsReadCommand>
    {
        public MarkMessageAsReadCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class MarkMessageAsReadCommandHandler : IRequestHandler<MarkMessageAsReadCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<MarkMessageAsReadCommandHandler> _logger;

        public MarkMessageAsReadCommandHandler(
            IUnitOfWork unitOfWork,
            IEventBus eventBus,
            ISignalRNotificationService signalRNotificationService,
            ILogger<MarkMessageAsReadCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _eventBus=eventBus;
            _signalRNotificationService=signalRNotificationService;
            _logger=logger;
        }


        public async Task<Result> Handle(
            MarkMessageAsReadCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Marking message {MessageId} as read", request.MessageId);

                var message = await _unitOfWork.Messages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} was not found");

                // Only receiver can mark message as read
                if(message.ReceiverId!= request.UserId)
                {
                    return Result.Failure("You can only mark messages sent to you  as read");
                }

                if (!message.IsRead)
                {
                    message.MarkAsRead();

                    // EF Core change tracker will automatically detect the property changes
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Send real-time read receipt to sender
                    await _signalRNotificationService.NotifyMessageReadAsync(
                        message.ConversationId,
                        message.Id,
                        request.UserId);


                    // Publish domain event for internal processing
                    await _eventBus.PublishAsync(
                        new MessageReadEvent(
                            message.Id,
                            message.ConversationId,
                            request.UserId,
                            DateTime.UtcNow),
                        cancellationToken);

                    _logger.LogInformation("Message {MessageId} marked as read", request.MessageId);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message {MessageId} as read", request.MessageId);
                return Result.Failure(ex.Message);
            }
        }
    }
}