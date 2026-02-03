using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.MessageConditions
{
    public record PinMessageCommand(
        Guid MessageId,
        Guid RequestedBy
    ) : IRequest<Result>;

    public class PinMessageCommandValidator : AbstractValidator<PinMessageCommand>
    {
        public PinMessageCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }

    public class PinMessageCommandHandler : IRequestHandler<PinMessageCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<PinMessageCommandHandler> _logger;

        public PinMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<PinMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService = signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result> Handle(
            PinMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Pinning message {MessageId}", request.MessageId);

                var message = await _unitOfWork.ChannelMessages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Verify user is a member of the channel
                var member = await _unitOfWork.ChannelMembers.GetMemberAsync(
                    message.ChannelId,
                    request.RequestedBy,
                    cancellationToken);

                if (member == null)
                    return Result.Failure("You must be a member of this channel to pin messages");

                if (message.IsPinned)
                {
                    return Result.Failure("Message is already pinned");
                }

                message.Pin(request.RequestedBy);

                await _unitOfWork.ChannelMessages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Send SignalR notification to channel members
                var memberUserIds = await _unitOfWork.ChannelMembers.GetChannelMemberIdsAsync(
                    message.ChannelId,
                    cancellationToken);
                var messageDto = await _unitOfWork.ChannelMessages.GetByIdAsDtoAsync(message.Id, cancellationToken);
                if (messageDto != null)
                {
                    await _signalRNotificationService.NotifyChannelMessagePinnedToMembersAsync(
                        message.ChannelId,
                        memberUserIds,
                        messageDto);
                }

                _logger?.LogInformation("Message {MessageId} pinned successfully", request.MessageId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error pinning message {MessageId}", request.MessageId);
                return Result.Failure(ex.Message);
            }
        }
    }
}