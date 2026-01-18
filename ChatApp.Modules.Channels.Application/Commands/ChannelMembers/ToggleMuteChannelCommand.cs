using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMembers
{
    public record ToggleMuteChannelCommand(
        Guid ChannelId,
        Guid UserId
    ) : IRequest<Result<bool>>; // Returns true if muted, false if unmuted

    public class ToggleMuteChannelCommandValidator : AbstractValidator<ToggleMuteChannelCommand>
    {
        public ToggleMuteChannelCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class ToggleMuteChannelCommandHandler : IRequestHandler<ToggleMuteChannelCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ToggleMuteChannelCommandHandler> _logger;

        public ToggleMuteChannelCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<ToggleMuteChannelCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            ToggleMuteChannelCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Verify user is a member of the channel
                var member = await _unitOfWork.ChannelMembers.GetMemberAsync(
                    request.ChannelId,
                    request.UserId,
                    cancellationToken);

                if (member == null)
                    return Result.Failure<bool>("User is not a member of this channel");

                member.ToggleMute();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Channel {ChannelId} mute toggled to {IsMuted} for user {UserId}",
                    request.ChannelId,
                    member.IsMuted,
                    request.UserId);

                return Result.Success(member.IsMuted);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error toggling mute for channel {ChannelId} by user {UserId}",
                    request.ChannelId,
                    request.UserId);
                return Result.Failure<bool>(ex.Message);
            }
        }
    }
}