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

                var member = await _unitOfWork.ConversationMembers.GetByConversationAndUserAsync(
                    request.ConversationId,
                    request.UserId,
                    cancellationToken);

                if (member == null)
                    return Result.Failure<bool>("Conversation member not found");

                if (member.IsMarkedReadLater)
                {
                    member.UnmarkConversationAsReadLater();
                }
                else
                {
                    member.MarkConversationAsReadLater();
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Conversation {ConversationId} mark as read later toggled to {IsMarkedReadLater} for user {UserId}",
                    request.ConversationId,
                    member.IsMarkedReadLater,
                    request.UserId);

                return Result.Success(member.IsMarkedReadLater);
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