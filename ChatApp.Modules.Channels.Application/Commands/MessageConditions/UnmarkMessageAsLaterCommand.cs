using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.MessageConditions
{
    public record UnmarkMessageAsLaterCommand(
        Guid ChannelId,
        Guid RequestedBy
    ) : IRequest<Result>;

    public class UnmarkMessageAsLaterCommandValidator : AbstractValidator<UnmarkMessageAsLaterCommand>
    {
        public UnmarkMessageAsLaterCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }

    public class UnmarkMessageAsLaterCommandHandler : IRequestHandler<UnmarkMessageAsLaterCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UnmarkMessageAsLaterCommandHandler> _logger;

        public UnmarkMessageAsLaterCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UnmarkMessageAsLaterCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            UnmarkMessageAsLaterCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation(
                    "Unmarking read later for user {UserId} in channel {ChannelId} (channel leave)",
                    request.RequestedBy,
                    request.ChannelId);

                // Get channel member
                var channelMember = await _unitOfWork.ChannelMembers.GetMemberAsync(
                    request.ChannelId,
                    request.RequestedBy,
                    cancellationToken);

                if (channelMember == null || !channelMember.IsActive)
                {
                    // User not member or inactive - no action needed
                    return Result.Success();
                }

                // If no read later mark exists, no action needed
                if (!channelMember.LastReadLaterMessageId.HasValue)
                {
                    return Result.Success();
                }

                // Clear read later mark
                channelMember.UnmarkMessageAsLater();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Read later unmarked successfully for user {UserId}",
                    request.RequestedBy);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error unmarking read later for user {UserId}",
                    request.RequestedBy);
                return Result.Failure(ex.Message);
            }
        }
    }
}
