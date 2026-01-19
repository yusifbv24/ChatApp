using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMembers
{
    public record UnmarkChannelReadLaterCommand(
        Guid ChannelId,
        Guid UserId
    ) : IRequest<Result>;

    public class UnmarkChannelReadLaterCommandValidator : AbstractValidator<UnmarkChannelReadLaterCommand>
    {
        public UnmarkChannelReadLaterCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class UnmarkChannelReadLaterCommandHandler : IRequestHandler<UnmarkChannelReadLaterCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UnmarkChannelReadLaterCommandHandler> _logger;

        public UnmarkChannelReadLaterCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UnmarkChannelReadLaterCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            UnmarkChannelReadLaterCommand request,
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
                    return Result.Failure("User is not a member of this channel");

                _logger?.LogInformation(
                    "[BEFORE UNMARK] Channel {ChannelId} User {UserId}: IsMarkedReadLater={IsMarkedReadLater}, LastReadLaterMessageId={LastReadLaterMessageId}",
                    request.ChannelId,
                    request.UserId,
                    member.IsMarkedReadLater,
                    member.LastReadLaterMessageId);

                // Clear both conversation-level and message-level marks
                member.UnmarkConversationAsReadLater(); // IsMarkedReadLater = false
                member.UnmarkMessageAsLater();          // LastReadLaterMessageId = null

                var savedChanges = await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "[AFTER UNMARK] Channel {ChannelId} User {UserId}: SaveChangesAsync returned {SavedChanges} entities updated",
                    request.ChannelId,
                    request.UserId,
                    savedChanges);

                _logger?.LogInformation(
                    "Channel {ChannelId} read later marks cleared for user {UserId}",
                    request.ChannelId,
                    request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error unmarking channel {ChannelId} as read later for user {UserId}",
                    request.ChannelId,
                    request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}
