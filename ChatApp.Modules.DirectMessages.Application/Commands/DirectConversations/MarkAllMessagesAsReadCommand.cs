using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectConversations
{
    public record MarkAllMessagesAsReadCommand(
        Guid ConversationId,
        Guid UserId
    ) : IRequest<Result<int>>; // Returns count of marked messages

    public class MarkAllMessagesAsReadCommandValidator : AbstractValidator<MarkAllMessagesAsReadCommand>
    {
        public MarkAllMessagesAsReadCommandValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("Conversation ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class MarkAllMessagesAsReadCommandHandler : IRequestHandler<MarkAllMessagesAsReadCommand, Result<int>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MarkAllMessagesAsReadCommandHandler> _logger;

        public MarkAllMessagesAsReadCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<MarkAllMessagesAsReadCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<int>> Handle(
            MarkAllMessagesAsReadCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation(
                    "Marking all messages as read in conversation {ConversationId} for user {UserId}",
                    request.ConversationId,
                    request.UserId);

                // Verify conversation exists and user is participant
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                    return Result.Failure<int>("Conversation not found");

                if (!conversation.IsParticipant(request.UserId))
                    return Result.Failure<int>("User is not a participant in this conversation");

                // Get member to clear LastReadLaterMessageId
                var member = await _unitOfWork.ConversationMembers.GetByConversationAndUserAsync(
                    request.ConversationId,
                    request.UserId,
                    cancellationToken);

                if (member == null)
                    return Result.Failure<int>("Conversation member not found");

                // Mark all unread messages as read
                var markedCount = await _unitOfWork.Messages.MarkAllAsReadAsync(
                    request.ConversationId,
                    request.UserId,
                    cancellationToken);

                // Clear LastReadLaterMessageId and IsMarkedReadLater
                member.UnmarkMessageAsLater();
                member.UnmarkConversationAsReadLater();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Marked {Count} messages as read in conversation {ConversationId} for user {UserId}",
                    markedCount,
                    request.ConversationId,
                    request.UserId);

                return Result.Success(markedCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error marking all messages as read in conversation {ConversationId} for user {UserId}",
                    request.ConversationId,
                    request.UserId);
                return Result.Failure<int>(ex.Message);
            }
        }
    }
}