using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.MessageConditions
{
    public record ToggleMessageAsLaterCommand(
        Guid ConversationId,
        Guid MessageId,
        Guid RequestedBy
    ) : IRequest<Result>;

    public class ToggleMessageAsLaterCommandValidator : AbstractValidator<ToggleMessageAsLaterCommand>
    {
        public ToggleMessageAsLaterCommandValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("Conversation ID is required");

            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }

    public class ToggleMessageAsLaterCommandHandler : IRequestHandler<ToggleMessageAsLaterCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ToggleMessageAsLaterCommandHandler> _logger;

        public ToggleMessageAsLaterCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<ToggleMessageAsLaterCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            ToggleMessageAsLaterCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation(
                    "Toggling message {MessageId} as later for user {UserId} in conversation {ConversationId}",
                    request.MessageId,
                    request.RequestedBy,
                    request.ConversationId);

                // Verify message exists and belongs to the conversation
                var message = await _unitOfWork.Messages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                if (message.ConversationId != request.ConversationId)
                    return Result.Failure("Message does not belong to the specified conversation");

                if (message.IsDeleted)
                    return Result.Failure("Cannot mark deleted messages as later");

                // Verify user is a participant in the conversation
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                    throw new NotFoundException($"Conversation with ID {request.ConversationId} not found");

                if (!conversation.IsParticipant(request.RequestedBy))
                    return Result.Failure("User is not a participant in this conversation");

                // Toggle logic:
                // If clicking on already marked message -> unmark it (toggle off)
                // If clicking on different message -> mark new one (auto-switches, previous mark is cleared)
                // If no mark exists -> mark this message
                var currentMarkedMessageId = conversation.GetLastReadLaterMessageId(request.RequestedBy);

                if (currentMarkedMessageId.HasValue && currentMarkedMessageId.Value == request.MessageId)
                {
                    // Toggle OFF: Unmark the message
                    conversation.UnmarkMessageAsLater(request.RequestedBy);
                    _logger?.LogInformation(
                        "Message {MessageId} unmarked as later for user {UserId}",
                        request.MessageId,
                        request.RequestedBy);
                }
                else
                {
                    // Toggle ON or SWITCH: Mark message as later (clears previous mark automatically)
                    conversation.MarkMessageAsLater(request.RequestedBy, request.MessageId);
                    _logger?.LogInformation(
                        "Message {MessageId} marked as later for user {UserId}",
                        request.MessageId,
                        request.RequestedBy);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error toggling message {MessageId} as later for user {UserId}",
                    request.MessageId,
                    request.RequestedBy);
                return Result.Failure(ex.Message);
            }
        }
    }
}
