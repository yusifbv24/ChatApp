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

                conversation.ToggleMute(request.UserId);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var isMuted = conversation.IsMuted(request.UserId);

                _logger?.LogInformation(
                    "Conversation {ConversationId} mute toggled to {IsMuted} for user {UserId}",
                    request.ConversationId,
                    isMuted,
                    request.UserId);

                return Result.Success(isMuted);
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
