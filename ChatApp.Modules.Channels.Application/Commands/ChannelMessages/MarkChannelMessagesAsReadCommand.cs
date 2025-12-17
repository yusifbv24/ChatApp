using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMessages
{
    public record MarkChannelMessagesAsReadCommand(
        Guid ChannelId,
        Guid UserId
    ) : IRequest<Result>;


    public class MarkChannelMessagesAsReadCommandValidator : AbstractValidator<MarkChannelMessagesAsReadCommand>
    {
        public MarkChannelMessagesAsReadCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }


    public class MarkChannelMessagesAsReadCommandHandler : IRequestHandler<MarkChannelMessagesAsReadCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<MarkChannelMessagesAsReadCommandHandler> _logger;

        public MarkChannelMessagesAsReadCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<MarkChannelMessagesAsReadCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService = signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result> Handle(
            MarkChannelMessagesAsReadCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get member
                var member = await _unitOfWork.ChannelMembers.GetMemberAsync(
                    request.ChannelId,
                    request.UserId,
                    cancellationToken);

                if (member == null || !member.IsActive)
                {
                    return Result.Failure("User is not a member of this channel");
                }

                // Mark as read (entity is already tracked, no need to call UpdateAsync)
                member.MarkAsRead();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Broadcast read status update to all channel members
                await _signalRNotificationService.NotifyChannelMessagesReadAsync(
                    request.ChannelId,
                    request.UserId,
                    member.LastReadAtUtc!.Value);

                _logger?.LogDebug(
                    "Messages marked as read for user {UserId} in channel {ChannelId}",
                    request.UserId,
                    request.ChannelId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error marking messages as read for user {UserId} in channel {ChannelId}",
                    request.UserId,
                    request.ChannelId);
                return Result.Failure(ex.Message);
            }
        }
    }
}
