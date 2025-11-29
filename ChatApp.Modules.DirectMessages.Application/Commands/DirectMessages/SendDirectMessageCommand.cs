using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using ChatApp.Modules.DirectMessages.Domain.Events;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectMessages
{
    public record SendDirectMessageCommand(
        Guid ConversationId,
        Guid SenderId,
        string Content,
        string? FileId=null
    ):IRequest<Result<Guid>>;



    public class SendDirectMessageCommandValidator : AbstractValidator<SendDirectMessageCommand>
    {
        public SendDirectMessageCommandValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("Conversation ID is required");

            RuleFor(x => x.SenderId)
                .NotEmpty().WithMessage("Sender ID is required");

            RuleFor(x => x.Content)
                .MaximumLength(4000).WithMessage("Message content cannot exceed 4000 characters");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Content) || !string.IsNullOrWhiteSpace(x.FileId))
                .WithMessage("Message must have content or file attachment");
        }
    }


    public class SendDirectMessageCommandHandler : IRequestHandler<SendDirectMessageCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<SendDirectMessageCommandHandler> _logger;

        public SendDirectMessageCommandHandler(
            IUnitOfWork unitOfWork,
            IEventBus eventBus,
            ISignalRNotificationService signalRNotificationService,
            ILogger<SendDirectMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _eventBus = eventBus;
            _signalRNotificationService= signalRNotificationService;
            _logger = logger;
        }


        public async Task<Result<Guid>> Handle(
            SendDirectMessageCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Sending message in conversation {ConversationId}",
                    request.ConversationId);

                // Get conversation
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                    throw new NotFoundException($"Conversation with ID {request.ConversationId} not found");

                // Verify sender is participant
                if (!conversation.IsParticipant(request.SenderId))
                {
                    return Result.Failure<Guid>("You are not a participant in this conversation");
                }

                // Get receiver ID
                var receiverId = conversation.GetOtherUserId(request.SenderId);

                // Create message
                var message = new DirectMessage(
                    request.ConversationId,
                    request.SenderId,
                    receiverId,
                    request.Content,
                    request.FileId);

                await _unitOfWork.Messages.AddAsync(message, cancellationToken);

                // Update conversation last message time and mark as having messages
                conversation.UpdateLastMessageTime();
                if (!conversation.HasMessages)
                {
                    conversation.MarkAsHasMessages();
                }
                await _unitOfWork.Conversations.UpdateAsync(conversation, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Get the full message DTO with user details for real-time broadcast
                var messages = await _unitOfWork.Messages.GetConversationMessagesAsync(
                    request.ConversationId,
                    pageSize: 1,
                    beforeUtc: null,
                    cancellationToken);

                var messageDto = messages.FirstOrDefault();

                if (messageDto != null)
                {
                    // Send real-time notification to receiver
                    await _signalRNotificationService.NotifyDirectMessageAsync(
                        request.ConversationId,
                        receiverId,
                        messageDto);
                }


                // Publish domain event for internal backend processing (email notifications, etc.)
                await _eventBus.PublishAsync(
                    new MessageSentEvent(
                        message.Id,
                        request.ConversationId,
                        request.SenderId,
                        receiverId,
                        request.Content,
                        message.CreatedAtUtc),
                    cancellationToken);

                _logger?.LogInformation("Message {MessageId} sent succesfully", message.Id);

                return Result.Success(message.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending message");
                return Result.Failure<Guid>("An error occurred while sending message");
            }
        }
    }
}