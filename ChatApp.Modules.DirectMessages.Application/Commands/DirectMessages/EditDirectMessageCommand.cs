using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectMessages
{
    public record EditDirectMessageCommand(
        Guid MessageId,
        string NewContent,
        Guid RequestedBy
    ):IRequest<Result>;


    public class EditDirectMessageCommandValidator : AbstractValidator<EditDirectMessageCommand>
    {
        public EditDirectMessageCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.NewContent)
                .NotEmpty().WithMessage("Message content cannot be empty")
                .MaximumLength(4000).WithMessage("Message content cannot exceed 4000 characters");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }


    public class EditDirectMessageCommandHandler: IRequestHandler<EditDirectMessageCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<EditDirectMessageCommandHandler> _logger;

        public EditDirectMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<EditDirectMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService= signalRNotificationService;
            _logger = logger;
        }


        public async Task<Result> Handle(
            EditDirectMessageCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Editing message {MessageId}", request.MessageId);

                var message = await _unitOfWork.Messages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Only sender can edit their own message
                if(message.SenderId != request.RequestedBy)
                {
                    return Result.Failure("You can only edit your own messages");
                }

                if (message.IsDeleted)
                {
                    return Result.Failure("Cannot edit deleted message");
                }

                var conversationId=message.ConversationId;
                var receiverId=message.ReceiverId;
                var senderId = message.SenderId;

                // Edit the message
                message.Edit(request.NewContent);

                // EF Core change tracker will automatically detect the property changes
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create updated message DTO manually (since GetConversationMessagesAsync only returns latest message)
                var messageDto = new DirectMessageDto(
                    Id: message.Id,
                    ConversationId: message.ConversationId,
                    SenderId: senderId,
                    SenderUsername: string.Empty, // Will be populated by frontend from user cache
                    SenderDisplayName: string.Empty, // Will be populated by frontend from user cache
                    SenderAvatarUrl: null, // Will be populated by frontend from user cache
                    ReceiverId: receiverId,
                    Content: message.Content,
                    FileId: message.FileId,
                    IsEdited: message.IsEdited,
                    IsDeleted: message.IsDeleted,
                    IsRead: message.IsRead,
                    ReactionCount: 0, // Not needed for edit notification
                    CreatedAtUtc: message.CreatedAtUtc,
                    EditedAtUtc: message.EditedAtUtc,
                    ReadAtUtc: message.ReadAtUtc,
                    ReplyToMessageId: message.ReplyToMessageId,
                    ReplyToContent: null, // We don't need this for edit notification
                    ReplyToSenderName: null,
                    IsForwarded: message.IsForwarded,
                    Reactions: new List<DirectMessageReactionDto>() // Empty for now
                );

                // Send real-time notification to receiver with edited message
                await _signalRNotificationService.NotifyDirectMessageEditedAsync(
                    conversationId,
                    receiverId,
                    messageDto);

                _logger?.LogInformation("Message {MessageId} edited succesfully", request.MessageId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error editing message {MessageId}", request.MessageId);
                return Result.Failure(ex.Message);
            }
        }
    }
}