using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMessages
{
    public record EditChannelMessageCommand(
        Guid MessageId,
        string NewContent,
        Guid RequestedBy
    ) : IRequest<Result>;


    public class EditChannelMessageCommandValidator : AbstractValidator<EditChannelMessageCommand>
    {
        public EditChannelMessageCommandValidator()
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


    public class EditChannelMessageCommandHandler : IRequestHandler<EditChannelMessageCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<EditChannelMessageCommandHandler> _logger;

        public EditChannelMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<EditChannelMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService= signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result> Handle(
            EditChannelMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Editing message {MessageId}", request.MessageId);

                var message = await _unitOfWork.ChannelMessages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Only sender can edit their own message
                if (message.SenderId != request.RequestedBy)
                {
                    return Result.Failure("You can only edit your own messages");
                }

                if (message.IsDeleted)
                {
                    return Result.Failure("Cannot edit deleted message");
                }

                var channelId=message.ChannelId;
                var senderId = message.SenderId;

                // Edit the message
                message.Edit(request.NewContent);

                await _unitOfWork.ChannelMessages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create updated message DTO manually (since GetChannelMessagesAsync only returns latest message)
                var messageDto = new ChatApp.Modules.Channels.Application.DTOs.Responses.ChannelMessageDto(
                    Id: message.Id,
                    ChannelId: message.ChannelId,
                    SenderId: senderId,
                    SenderUsername: string.Empty, // Will be populated by frontend from user cache
                    SenderDisplayName: string.Empty, // Will be populated by frontend from user cache
                    SenderAvatarUrl: null, // Will be populated by frontend from user cache
                    Content: message.Content,
                    FileId: message.FileId,
                    IsEdited: message.IsEdited,
                    IsDeleted: message.IsDeleted,
                    IsPinned: message.IsPinned,
                    ReactionCount: 0, // Not needed for edit notification
                    CreatedAtUtc: message.CreatedAtUtc,
                    EditedAtUtc: message.EditedAtUtc,
                    PinnedAtUtc: message.PinnedAtUtc,
                    ReplyToMessageId: message.ReplyToMessageId,
                    ReplyToContent: null, // We don't need this for edit notification
                    ReplyToSenderName: null,
                    IsForwarded: message.IsForwarded
                );

                // Send real-time notification with edited message
                await _signalRNotificationService.NotifyChannelMessageEditedAsync(channelId, messageDto);

                _logger?.LogInformation("Message {MessageId} edited successfully", request.MessageId);

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