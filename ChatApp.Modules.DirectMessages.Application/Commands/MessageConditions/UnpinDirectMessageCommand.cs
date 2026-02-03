using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.MessageConditions
{
    public record UnpinDirectMessageCommand(
        Guid MessageId,
        Guid RequestedBy
    ) : IRequest<Result>;

    public class UnpinDirectMessageCommandValidator : AbstractValidator<UnpinDirectMessageCommand>
    {
        public UnpinDirectMessageCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }

    public class UnpinDirectMessageCommandHandler : IRequestHandler<UnpinDirectMessageCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<UnpinDirectMessageCommandHandler> _logger;

        public UnpinDirectMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<UnpinDirectMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService = signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result> Handle(
            UnpinDirectMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Unpinning direct message {MessageId}", request.MessageId);

                var message = await _unitOfWork.Messages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken)
                    ?? throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Check if user is part of the conversation
                if (message.SenderId != request.RequestedBy && message.ReceiverId != request.RequestedBy)
                {
                    return Result.Failure("You can only unpin messages in your own conversations");
                }

                if (!message.IsPinned)
                {
                    return Result.Failure("Message is not pinned");
                }

                message.Unpin();

                await _unitOfWork.Messages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Send SignalR notification to other participant
                var receiverId = message.SenderId == request.RequestedBy ? message.ReceiverId : message.SenderId;
                var messageDto = await _unitOfWork.Messages.GetByIdAsDtoAsync(message.Id, cancellationToken);
                if (messageDto != null)
                {
                    await _signalRNotificationService.NotifyDirectMessageUnpinnedAsync(
                        message.ConversationId,
                        receiverId,
                        messageDto);
                }

                _logger?.LogInformation("Direct message {MessageId} unpinned successfully", request.MessageId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error unpinning direct message {MessageId}", request.MessageId);
                return Result.Failure(ex.Message);
            }
        }
    }
}
