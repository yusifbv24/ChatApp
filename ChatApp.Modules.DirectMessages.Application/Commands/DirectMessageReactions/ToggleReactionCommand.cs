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
    public record ToggleReactionCommand(
        Guid MessageId,
        Guid UserId,
        string Reaction
    ) : IRequest<Result<ReactionToggleResult>>;

    public record ReactionToggleResult(
        bool WasAdded,
        bool WasRemoved,
        bool WasReplaced,
        string Reaction,
        List<ReactionSummary> Reactions
    );

    public record ReactionSummary(
        string Emoji,
        int Count,
        List<Guid> UserIds,
        List<string> UserDisplayNames,
        List<string?> UserAvatarUrls
    );

    public class ToggleReactionCommandValidator : AbstractValidator<ToggleReactionCommand>
    {
        public ToggleReactionCommandValidator()
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

    public class ToggleReactionCommandHandler : IRequestHandler<ToggleReactionCommand, Result<ReactionToggleResult>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<ToggleReactionCommandHandler> _logger;

        public ToggleReactionCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<ToggleReactionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService = signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result<ReactionToggleResult>> Handle(
            ToggleReactionCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Toggling reaction {Reaction} on message {MessageId} by user {UserId}",
                    request.Reaction,
                    request.MessageId,
                    request.UserId);

                var message = await _unitOfWork.Messages.GetByIdWithReactionsAsync(
                    request.MessageId,
                    cancellationToken)
                        ?? throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Prevent reactions on deleted messages
                if (message.IsDeleted)
                {
                    return Result.Failure<ReactionToggleResult>("Cannot react to deleted messages");
                }

                // Verify user is participant in the conversation
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    message.ConversationId,
                    cancellationToken);

                if (conversation == null || !conversation.IsParticipant(request.UserId))
                {
                    return Result.Failure<ReactionToggleResult>("You must be a participant to react to messages");
                }

                bool wasAdded = false;
                bool wasRemoved = false;
                bool wasReplaced = false;

                // Find existing reaction from this user (any emoji)
                var existingReaction = message.Reactions.FirstOrDefault(r => r.UserId == request.UserId);

                if (existingReaction != null)
                {
                    if (existingReaction.Reaction == request.Reaction)
                    {
                        // Same emoji - toggle off (remove)
                        message.RemoveReaction(request.UserId, request.Reaction);
                        await _unitOfWork.Reactions.DeleteAsync(existingReaction, cancellationToken);
                        wasRemoved = true;

                        _logger.LogInformation(
                            "Removed reaction {Reaction} from message {MessageId}",
                            request.Reaction,
                            request.MessageId);
                    }
                    else
                    {
                        // Different emoji - replace (remove old, add new)
                        message.RemoveReaction(request.UserId, existingReaction.Reaction);
                        await _unitOfWork.Reactions.DeleteAsync(existingReaction, cancellationToken);

                        var newReaction = new DirectMessageReaction(
                            request.MessageId,
                            request.UserId,
                            request.Reaction);

                        await _unitOfWork.Reactions.AddAsync(newReaction, cancellationToken);
                        wasReplaced = true;

                        _logger.LogInformation(
                            "Replaced reaction {OldReaction} with {NewReaction} on message {MessageId}",
                            existingReaction.Reaction,
                            request.Reaction,
                            request.MessageId);
                    }
                }
                else
                {
                    // No existing reaction - add new
                    var reaction = new DirectMessageReaction(
                        request.MessageId,
                        request.UserId,
                        request.Reaction);

                    await _unitOfWork.Reactions.AddAsync(reaction, cancellationToken);
                    wasAdded = true;

                    _logger.LogInformation(
                        "Added reaction {Reaction} to message {MessageId}",
                        request.Reaction,
                        request.MessageId);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Get updated reactions with user details
                var reactionSummary = await _unitOfWork.Reactions.GetMessageReactionsWithUserDetailsAsync(
                    request.MessageId,
                    cancellationToken);

                var result = new ReactionToggleResult(
                    wasAdded,
                    wasRemoved,
                    wasReplaced,
                    request.Reaction,
                    reactionSummary);

                // Send real-time notification to other participant
                var otherUserId = conversation.GetOtherUserId(request.UserId);
                await _signalRNotificationService.NotifyUserAsync(
                    otherUserId,
                    "DirectMessageReactionToggled",
                    new
                    {
                        conversationId = message.ConversationId,
                        messageId = request.MessageId,
                        userId = request.UserId,
                        wasAdded,
                        wasRemoved,
                        wasReplaced,
                        reaction = request.Reaction,
                        reactions = reactionSummary
                    });

                return Result.Success(result);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<ReactionToggleResult>(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error toggling reaction on message {MessageId}",
                    request.MessageId);
                return Result.Failure<ReactionToggleResult>("An error occurred while toggling the reaction");
            }
        }
    }
}