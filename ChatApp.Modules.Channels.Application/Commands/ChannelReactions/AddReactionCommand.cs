using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelReactions
{
    public record AddReactionCommand(
        Guid MessageId,
        Guid UserId,
        string Reaction
    ) : IRequest<Result>;

    public class AddReactionCommandValidator : AbstractValidator<AddReactionCommand>
    {
        public AddReactionCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Reaction)
                .NotEmpty().WithMessage("Reaction cannot be empty")
                .MaximumLength(10).WithMessage("Reaction must be a single emoji");
        }
    }

    public class AddReactionCommandHandler : IRequestHandler<AddReactionCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AddReactionCommandHandler> _logger;

        public AddReactionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<AddReactionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            AddReactionCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Adding reaction {Reaction} to message {MessageId}",
                    request.Reaction,
                    request.MessageId);

                var message = await _unitOfWork.ChannelMessages.GetByIdWithReactionsAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Verify user is a member
                var isMember = await _unitOfWork.Channels.IsUserMemberAsync(
                    message.ChannelId,
                    request.UserId,
                    cancellationToken);

                if (!isMember)
                {
                    return Result.Failure("You must be a member to react to messages");
                }

                var reaction = new ChannelMessageReaction(
                    request.MessageId,
                    request.UserId,
                    request.Reaction);

                message.AddReaction(reaction);

                await _unitOfWork.ChannelMessages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Reaction {Reaction} added to message {MessageId} successfully",
                    request.Reaction,
                    request.MessageId);

                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error adding reaction to message {MessageId}",
                    request.MessageId);
                return Result.Failure("An error occurred while adding the reaction");
            }
        }
    }
}