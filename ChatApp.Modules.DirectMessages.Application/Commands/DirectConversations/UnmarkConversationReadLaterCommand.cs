using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectConversations
{
    /// <summary>
    /// Command to clear all "mark as read later" flags when user opens the conversation
    /// Clears both conversation-level (IsMarkedReadLater) and message-level (LastReadLaterMessageId)
    /// This removes the icon from conversation list but does NOT mark messages as read
    /// </summary>
    public record UnmarkConversationReadLaterCommand(
        Guid ConversationId,
        Guid UserId
    ) : IRequest<Result>;

    public class UnmarkConversationReadLaterCommandValidator : AbstractValidator<UnmarkConversationReadLaterCommand>
    {
        public UnmarkConversationReadLaterCommandValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("Conversation ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class UnmarkConversationReadLaterCommandHandler : IRequestHandler<UnmarkConversationReadLaterCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UnmarkConversationReadLaterCommandHandler> _logger;

        public UnmarkConversationReadLaterCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UnmarkConversationReadLaterCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            UnmarkConversationReadLaterCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                    return Result.Failure("Conversation not found");

                if (!conversation.IsParticipant(request.UserId))
                    return Result.Failure("User is not a participant in this conversation");

                var member = await _unitOfWork.ConversationMembers.GetByConversationAndUserAsync(
                    request.ConversationId,
                    request.UserId,
                    cancellationToken);

                if (member == null)
                    return Result.Failure("Conversation member not found");

                // Clear both conversation-level and message-level marks when opening conversation
                // This removes the icon from conversation list
                var hasChanges = member.IsMarkedReadLater || member.LastReadLaterMessageId.HasValue;

                if (hasChanges)
                {
                    member.UnmarkConversationAsReadLater(); // Clear IsMarkedReadLater
                    member.UnmarkMessageAsLater();          // Clear LastReadLaterMessageId
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger?.LogInformation(
                        "Cleared read later marks for conversation {ConversationId} and user {UserId}",
                        request.ConversationId,
                        request.UserId);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error unmarking conversation {ConversationId} as read later for user {UserId}",
                    request.ConversationId,
                    request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}