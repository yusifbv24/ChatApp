using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectMessageReactions
{
    public record RemoveReactionCommand(
        Guid MessageId,
        Guid UserId,
        string Reaction
    ):IRequest<Result>;

    
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

    public class RemoveReactionCommandHandler : IRequestHandler<RemoveReactionCommand,Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<RemoveReactionCommandHandler> _logger;

        public RemoveReactionCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<RemoveReactionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService=signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result> Handle(
            RemoveReactionCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Removing reaction {Reaction} from message {MessageId}",
                    request.Reaction,
                    request.MessageId);

                var message = await _unitOfWork.Messages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Verify user is participant
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    message.ConversationId,
                    cancellationToken);

                if(conversation==null || !conversation.IsParticipant(request.UserId))
                {
                    return Result.Failure("You must be a participant to remove reactions");
                }

                message.RemoveReaction(request.UserId,request.Reaction);

                // EF Core change tracker will automatically detect the removed reaction
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Send real-time notifications to other participant
                var otherUserId=conversation.GetOtherUserId(request.UserId);
                await _signalRNotificationService.NotifyUserAsync(
                    otherUserId,
                    "DirectMessageReactionRemoved",
                    new
                    {
                        conversationId = message.ConversationId,
                        messageId = request.MessageId,
                        userId = request.UserId,
                        reaction = request.Reaction
                    });

                _logger?.LogInformation(
                    "Reaction {Reaction} removed from message {MessageId} succesfully",
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