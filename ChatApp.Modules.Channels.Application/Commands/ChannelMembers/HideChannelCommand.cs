using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMembers
{
    public record HideChannelCommand(
        Guid ChannelId,
        Guid UserId
    ) : IRequest<Result>;

    public class HideChannelCommandValidator : AbstractValidator<HideChannelCommand>
    {
        public HideChannelCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class HideChannelCommandHandler : IRequestHandler<HideChannelCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HideChannelCommandHandler> _logger;

        public HideChannelCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<HideChannelCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            HideChannelCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var member = await _unitOfWork.ChannelMembers.GetMemberAsync(
                    request.ChannelId,
                    request.UserId,
                    cancellationToken);

                if (member == null)
                    return Result.Failure("User is not a member of this channel");

                // Mesaj yoxdursa hide etmək olmaz
                var hasMessages = await _unitOfWork.ChannelMessages.HasMessagesAsync(
                    request.ChannelId,
                    cancellationToken);

                if (!hasMessages)
                    return Result.Failure("Cannot hide channel without messages");

                member.Hide();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Channel {ChannelId} hidden for user {UserId}",
                    request.ChannelId,
                    request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error hiding channel {ChannelId} for user {UserId}",
                    request.ChannelId,
                    request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}
