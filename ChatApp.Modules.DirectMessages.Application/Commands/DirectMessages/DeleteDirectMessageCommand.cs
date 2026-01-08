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

                var conversationId = message.ConversationId;
                var receiverId = message.ReceiverId;
                var senderId = message.SenderId;

                // Delete the message (soft delete)
                message.Delete();

                // EF Core change tracker will automatically detect the property changes
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create deleted message DTO manually (showing deleted state)
                var messageDto = new DirectMessageDto(
                    Id: message.Id,
                    ConversationId: message.ConversationId,
                    SenderId: senderId,
                    SenderUsername: string.Empty, // Will be populated by frontend from user cache
                    SenderDisplayName: string.Empty, // Will be populated by frontend from user cache
                    SenderAvatarUrl: null, // Will be populated by frontend from user cache
                    ReceiverId: receiverId,
                    Content: message.Content, // Content preserved in backend but shown as "deleted" in frontend
                    FileId: message.FileId,
                    FileName: null, // File metadata not needed for deleted messages
                    FileContentType: null, // File metadata not needed for deleted messages
                    FileSizeInBytes: null, // File metadata not needed for deleted messages
                    IsEdited: message.IsEdited,
                    IsDeleted: true, // Mark as deleted
                    IsRead: message.IsRead,
                    IsPinned: message.IsPinned,
                    ReactionCount: 0,
                    CreatedAtUtc: message.CreatedAtUtc,
                    EditedAtUtc: message.EditedAtUtc,
                    PinnedAtUtc: message.PinnedAtUtc,
                    ReplyToMessageId: message.ReplyToMessageId,
                    ReplyToContent: null,
                    ReplyToSenderName: null,
                    ReplyToFileId: null,
                    ReplyToFileName: null,
                    ReplyToFileContentType: null,
                    IsForwarded: message.IsForwarded,
                    Reactions: new List<DirectMessageReactionDto>()
                );

                // Send real-time notification to receiver with deleted message DTO
                await _signalRNotificationService.NotifyDirectMessageDeletedAsync(
                    conversationId,
                    receiverId,
                    messageDto);

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