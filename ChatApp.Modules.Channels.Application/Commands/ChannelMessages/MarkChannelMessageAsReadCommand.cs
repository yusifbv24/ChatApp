using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMessages
{
    public record MarkChannelMessageAsReadCommand(
        Guid MessageId,
        Guid ChannelId,
        Guid UserId
    ) : IRequest<Result>;


    public class MarkChannelMessageAsReadCommandValidator : AbstractValidator<MarkChannelMessageAsReadCommand>
    {
        public MarkChannelMessageAsReadCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }


    public class MarkChannelMessageAsReadCommandHandler : IRequestHandler<MarkChannelMessageAsReadCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<MarkChannelMessageAsReadCommandHandler> _logger;

        public MarkChannelMessageAsReadCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<MarkChannelMessageAsReadCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService = signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result> Handle(
            MarkChannelMessageAsReadCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get member to verify they're in the channel
                var member = await _unitOfWork.ChannelMembers.GetMemberAsync(
                    request.ChannelId,
                    request.UserId,
                    cancellationToken);

                if (member == null || !member.IsActive)
                {
                    return Result.Failure("User is not a member of this channel");
                }

                // Get the message to verify it exists and user is not the sender
                var message = await _unitOfWork.ChannelMessages.GetByIdAsync(request.MessageId, cancellationToken);
                if (message == null)
                {
                    return Result.Failure("Message not found");
                }

                // Don't mark user's own messages as read
                if (message.SenderId == request.UserId)
                {
                    return Result.Success();
                }

                // Check if already marked as read
                var alreadyRead = await _unitOfWork.ChannelMessageReads
                    .ExistsAsync(request.MessageId, request.UserId, cancellationToken);

                if (alreadyRead)
                {
                    return Result.Success(); // Already marked, nothing to do
                }

                // Create and add the read record
                var readRecord = new ChannelMessageRead(request.MessageId, request.UserId);
                await _unitOfWork.ChannelMessageReads.AddAsync(readRecord, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Broadcast read status update to all channel members with the message ID
                await _signalRNotificationService.NotifyChannelMessagesReadAsync(
                    request.ChannelId,
                    request.UserId,
                    new List<Guid> { request.MessageId });

                _logger?.LogDebug(
                    "Message {MessageId} marked as read for user {UserId} in channel {ChannelId}",
                    request.MessageId,
                    request.UserId,
                    request.ChannelId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error marking message {MessageId} as read for user {UserId} in channel {ChannelId}",
                    request.MessageId,
                    request.UserId,
                    request.ChannelId);
                return Result.Failure(ex.Message);
            }
        }
    }
}