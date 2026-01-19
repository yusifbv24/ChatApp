using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectConversations
{
    public record ToggleMuteConversationCommand(
        Guid ConversationId,
        Guid UserId
    ) : IRequest<Result<bool>>; // Returns true if muted, false if unmuted

    public class ToggleMuteConversationCommandValidator : AbstractValidator<ToggleMuteConversationCommand>
    {
        public ToggleMuteConversationCommandValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("Conversation ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class ToggleMuteConversationCommandHandler : IRequestHandler<ToggleMuteConversationCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ToggleMuteConversationCommandHandler> _logger;

        public ToggleMuteConversationCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<ToggleMuteConversationCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            ToggleMuteConversationCommand request,
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

                member.ToggleMute();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Conversation {ConversationId} mute toggled to {IsMuted} for user {UserId}",
                    request.ConversationId,
                    member.IsMuted,
                    request.UserId);

                return Result.Success(member.IsMuted);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error toggling mute for conversation {ConversationId} by user {UserId}",
                    request.ConversationId,
                    request.UserId);
                return Result.Failure<bool>(ex.Message);
            }
        }
    }
}