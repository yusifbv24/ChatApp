using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Events;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectMessages
{
    public record MarkDirectMessagesAsReadCommand(
        Guid ConversationId,
        Guid UserId
    ) : IRequest<Result>;


    public class MarkDirectMessagesAsReadCommandValidator : AbstractValidator<MarkDirectMessagesAsReadCommand>
    {
        public MarkDirectMessagesAsReadCommandValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("Conversation ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }


    public class MarkDirectMessagesAsReadCommandHandler : IRequestHandler<MarkDirectMessagesAsReadCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<MarkDirectMessagesAsReadCommandHandler> _logger;

        public MarkDirectMessagesAsReadCommandHandler(
            IUnitOfWork unitOfWork,
            IEventBus eventBus,
            ISignalRNotificationService signalRNotificationService,
            ILogger<MarkDirectMessagesAsReadCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _eventBus = eventBus;
            _signalRNotificationService = signalRNotificationService;
            _logger = logger;
        }


        public async Task<Result> Handle(
            MarkDirectMessagesAsReadCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Marking all unread messages in conversation {ConversationId} as read for user {UserId}",
                    request.ConversationId,
                    request.UserId);

                // Get conversation
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                    return Result.Failure($"Conversation with ID {request.ConversationId} was not found");

                // Verify user is participant
                if (conversation.User1Id != request.UserId && conversation.User2Id != request.UserId)
                {
                    return Result.Failure("You are not a participant in this conversation");
                }

                // Get all unread messages in the conversation (where user is receiver and message is not read)
                var unreadMessages = await _unitOfWork.Messages.GetUnreadMessagesForUserAsync(
                    request.ConversationId,
                    request.UserId,
                    cancellationToken);

                if (unreadMessages.Any())
                {
                    // Mark all messages as read
                    foreach (var message in unreadMessages)
                    {
                        message.MarkAsRead();
                    }

                    // Save all changes in one transaction
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Send real-time notifications for each message
                    foreach (var message in unreadMessages)
                    {
                        await _signalRNotificationService.NotifyMessageReadAsync(
                            message.ConversationId,
                            message.Id,
                            request.UserId,
                            message.SenderId);

                        // Publish domain event for internal processing
                        await _eventBus.PublishAsync(
                            new MessageReadEvent(
                                message.Id,
                                message.ConversationId,
                                request.UserId),
                            cancellationToken);
                    }

                    _logger.LogInformation(
                        "Marked {Count} messages as read in conversation {ConversationId} for user {UserId}",
                        unreadMessages.Count,
                        request.ConversationId,
                        request.UserId);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error marking messages as read in conversation {ConversationId} for user {UserId}",
                    request.ConversationId,
                    request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}
