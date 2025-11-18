using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectMessageReactions
{
    public record AddReactionCommand(
        Guid MessageId,
        Guid UserId,
        string Reaction
    ):IRequest<Result>;


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
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<AddReactionCommandHandler> _logger;

        public AddReactionCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<AddReactionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService=signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result> Handle(
            AddReactionCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Adding reaction {Reaction} to message {MessageId}",
                    request.Reaction,
                    request.MessageId);

                var message = await _unitOfWork.Messages.GetByIdWithReactionsAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Verify user is participant in the conversation
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    message.ConversationId,
                    cancellationToken);

                if(conversation==null || !conversation.IsParticipant(request.UserId))
                {
                    return Result.Failure("You must be a participant to react to messages");
                }

                var reaction = new DirectMessageReaction(
                    request.MessageId,
                    request.UserId,
                    request.Reaction);

                message.AddReaction(reaction);

                await _unitOfWork.Messages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Send real-time notification to other participant
                var otherUserId = conversation.GetOtherUserId(request.UserId);
                await _signalRNotificationService.NotifyUserAsync(
                    otherUserId,
                    "DirectMessageReactionAdded",
                    new
                    {
                        conversationId = message.ConversationId,
                        messageId = request.MessageId,
                        userId = request.UserId,
                        reaction = request.Reaction
                    });

                _logger?.LogInformation(
                    "Reaction {Reaction} added to message {MessageId} succesfully",
                    request.Reaction,
                    request.MessageId);

                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch(Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error adding reaction to message {MessageId}",
                    request.MessageId);
                return Result.Failure("An error occurred while adding the reaction");
            }
        }
    }
}