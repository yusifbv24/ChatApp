using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using ChatApp.Modules.Channels.Domain.Events;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMessages
{
    public record SendChannelMessageCommand(
        Guid ChannelId,
        Guid SenderId,
        string Content,
        string? FileId = null,
        Guid? ReplyToMessageId = null,
        bool IsForwarded = false
    ) : IRequest<Result<Guid>>;



    public class SendChannelMessageCommandValidator : AbstractValidator<SendChannelMessageCommand>
    {
        public SendChannelMessageCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.SenderId)
                .NotEmpty().WithMessage("Sender ID is required");

            RuleFor(x => x.Content)
                .MaximumLength(4000).WithMessage("Message content cannot exceed 4000 characters");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Content) || !string.IsNullOrWhiteSpace(x.FileId))
                .WithMessage("Message must have content or file attachment");
        }
    }



    public class SendChannelMessageCommandHandler : IRequestHandler<SendChannelMessageCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<SendChannelMessageCommandHandler> _logger;

        public SendChannelMessageCommandHandler(
            IUnitOfWork unitOfWork,
            IEventBus eventBus,
            ISignalRNotificationService signalRNotificationService,
            ILogger<SendChannelMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _eventBus = eventBus;
            _signalRNotificationService = signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            SendChannelMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation(
                    "Sending message to channel {ChannelId} from user {SenderId}",
                    request.ChannelId,
                    request.SenderId);

                // Verify channel exists
                var channel = await _unitOfWork.Channels.GetByIdAsync(
                    request.ChannelId,
                    cancellationToken);

                if (channel == null)
                    throw new NotFoundException($"Channel with ID {request.ChannelId} not found");

                // Verify user is a member
                var isMember = await _unitOfWork.Channels.IsUserMemberAsync(
                    request.ChannelId,
                    request.SenderId,
                    cancellationToken);

                if (!isMember)
                {
                    return Result.Failure<Guid>("You must be a member to send messages to this channel");
                }

                // Create message
                var message = new ChannelMessage(
                    request.ChannelId,
                    request.SenderId,
                    request.Content,
                    request.FileId,
                    request.ReplyToMessageId,
                    request.IsForwarded);

                await _unitOfWork.ChannelMessages.AddAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Get the full message DTO with user details for real-time broadcast
                var messages = await _unitOfWork.ChannelMessages.GetChannelMessagesAsync(
                    request.ChannelId,
                    pageSize: 1,
                    beforeUtc: null,
                    cancellationToken);

                var messageDto = messages.FirstOrDefault();

                if(messageDto != null)
                {
                    // Send real-time notification to all users in the channel
                    await _signalRNotificationService.NotifyChannelMessageAsync(
                        request.ChannelId,
                        messageDto);
                }

                // Publish domain event (for other modules/event handlers)
                await _eventBus.PublishAsync(
                    new ChannelMessageSentEvent(
                        message.Id,
                        request.ChannelId,
                        request.SenderId,
                        request.Content),
                    cancellationToken);

                _logger?.LogInformation(
                    "Message {MessageId} sent to channel {ChannelId} successfully",
                    message.Id,
                    request.ChannelId);

                return Result.Success(message.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error sending message to channel {ChannelId}",
                    request.ChannelId);
                return Result.Failure<Guid>("An error occurred while sending the message");
            }
        }
    }
}