using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectMessages
{
    public record DeleteDirectMessageCommand(
        Guid MessageId,
        Guid RequestedBy
    ):IRequest<Result>;


    public class DeleteDirectMessageCommandValidator : AbstractValidator<DeleteDirectMessageCommand>
    {
        public DeleteDirectMessageCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");
            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }


    public class DeleteDirectMessageCommandHandler : IRequestHandler<DeleteDirectMessageCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<DeleteDirectMessageCommandHandler> _logger;

        public DeleteDirectMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<DeleteDirectMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService= signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result> Handle(
            DeleteDirectMessageCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting message {MessageId}", request.MessageId);

                var message = await _unitOfWork.Messages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Only sender can delete their own message
                if(message.SenderId != request.RequestedBy)
                {
                    return Result.Failure("You can delete your own messages");
                }

                var conversationId=message.ConversationId;
                var receiverId = message.ReceiverId;

                message.Delete();

                // EF Core change tracker will automatically detect the property changes
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Send real-time notification to receiver
                await _signalRNotificationService.NotifyUserAsync(
                    receiverId,
                    "DirectMessageDeleted",
                    new { conversationId, messageId = request.MessageId });

                _logger.LogInformation("Message {MessageId} deleted successfully", request.MessageId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", request.MessageId);
                return Result.Failure(ex.Message);
            }
        }
    }
}