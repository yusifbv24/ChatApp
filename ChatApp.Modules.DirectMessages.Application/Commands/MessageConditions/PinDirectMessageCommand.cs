using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.MessageConditions
{
    public record PinDirectMessageCommand(
        Guid MessageId,
        Guid RequestedBy
    ) : IRequest<Result>;

    public class PinDirectMessageCommandValidator : AbstractValidator<PinDirectMessageCommand>
    {
        public PinDirectMessageCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }

    public class PinDirectMessageCommandHandler : IRequestHandler<PinDirectMessageCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PinDirectMessageCommandHandler> _logger;

        public PinDirectMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<PinDirectMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            PinDirectMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Pinning direct message {MessageId}", request.MessageId);

                var message = await _unitOfWork.Messages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken) 
                    ?? throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Check if user is part of the conversation
                if (message.SenderId != request.RequestedBy && message.ReceiverId != request.RequestedBy)
                {
                    return Result.Failure("You can only pin messages in your own conversations");
                }

                if (message.IsPinned)
                {
                    return Result.Failure("Message is already pinned");
                }

                message.Pin(request.RequestedBy);

                await _unitOfWork.Messages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("Direct message {MessageId} pinned successfully", request.MessageId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error pinning direct message {MessageId}", request.MessageId);
                return Result.Failure(ex.Message);
            }
        }
    }
}
