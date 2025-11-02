using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelReactions
{
    public record RemoveReactionCommand(
        Guid MessageId,
        Guid UserId,
        string Reaction
    ) : IRequest<Result>;

    public class RemoveReactionCommandValidator : AbstractValidator<RemoveReactionCommand>
    {
        public RemoveReactionCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Reaction)
                .NotEmpty().WithMessage("Reaction cannot be empty");
        }
    }

    public class RemoveReactionCommandHandler : IRequestHandler<RemoveReactionCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveReactionCommandHandler> _logger;

        public RemoveReactionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<RemoveReactionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            RemoveReactionCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Removing reaction {Reaction} from message {MessageId}",
                    request.Reaction,
                    request.MessageId);

                var message = await _unitOfWork.ChannelMessages.GetByIdWithReactionsAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                message.RemoveReaction(request.UserId, request.Reaction);

                await _unitOfWork.ChannelMessages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Reaction {Reaction} removed from message {MessageId} successfully",
                    request.Reaction,
                    request.MessageId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error removing reaction from message {MessageId}",
                    request.MessageId);
                return Result.Failure("An error occurred while removing the reaction");
            }
        }
    }
}