using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectConversations
{
    public record ToggleMarkConversationAsReadLaterCommand(
        Guid ConversationId,
        Guid UserId
    ) : IRequest<Result<bool>>; // Returns true if marked as read later, false if unmarked

    public class ToggleMarkConversationAsReadLaterCommandValidator : AbstractValidator<ToggleMarkConversationAsReadLaterCommand>
    {
        public ToggleMarkConversationAsReadLaterCommandValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("Conversation ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class ToggleMarkConversationAsReadLaterCommandHandler : IRequestHandler<ToggleMarkConversationAsReadLaterCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ToggleMarkConversationAsReadLaterCommandHandler> _logger;

        public ToggleMarkConversationAsReadLaterCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<ToggleMarkConversationAsReadLaterCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            ToggleMarkConversationAsReadLaterCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                    return Result.Failure<bool>("Conversation not found");

                if (!conversation.IsParticipant(request.UserId))
                    return Result.Failure<bool>("User is not a participant in this conversation");

                if (conversation.IsMarkedReadLater(request.UserId))
                {
                    conversation.UnmarkConversationAsReadLater(request.UserId);
                }
                else
                {
                    conversation.MarkConversationAsReadLater(request.UserId);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var isMarkedReadLater = conversation.IsMarkedReadLater(request.UserId);

                _logger?.LogInformation(
                    "Conversation {ConversationId} mark as read later toggled to {IsMarkedReadLater} for user {UserId}",
                    request.ConversationId,
                    isMarkedReadLater,
                    request.UserId);

                return Result.Success(isMarkedReadLater);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error toggling mark as read later for conversation {ConversationId} by user {UserId}",
                    request.ConversationId,
                    request.UserId);
                return Result.Failure<bool>(ex.Message);
            }
        }
    }
}
