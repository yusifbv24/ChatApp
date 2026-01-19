using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMembers
{
    public record ToggleMarkChannelAsReadLaterCommand(
        Guid ChannelId,
        Guid UserId
    ) : IRequest<Result<bool>>; // Returns true if marked as read later, false if unmarked

    public class ToggleMarkChannelAsReadLaterCommandValidator : AbstractValidator<ToggleMarkChannelAsReadLaterCommand>
    {
        public ToggleMarkChannelAsReadLaterCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class ToggleMarkChannelAsReadLaterCommandHandler : IRequestHandler<ToggleMarkChannelAsReadLaterCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ToggleMarkChannelAsReadLaterCommandHandler> _logger;

        public ToggleMarkChannelAsReadLaterCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<ToggleMarkChannelAsReadLaterCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            ToggleMarkChannelAsReadLaterCommand request,
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

                var initialState = member.IsMarkedReadLater;
                _logger?.LogInformation(
                    "[BEFORE TOGGLE] Channel {ChannelId} User {UserId}: IsMarkedReadLater = {InitialState}",
                    request.ChannelId,
                    request.UserId,
                    initialState);

                if (member.IsMarkedReadLater)
                {
                    member.UnmarkConversationAsReadLater();
                }
                else
                {
                    member.MarkConversationAsReadLater();
                }

                var newState = member.IsMarkedReadLater;
                _logger?.LogInformation(
                    "[AFTER TOGGLE] Channel {ChannelId} User {UserId}: IsMarkedReadLater = {NewState}",
                    request.ChannelId,
                    request.UserId,
                    newState);

                var savedChanges = await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger?.LogInformation(
                    "[AFTER SAVE] Channel {ChannelId} User {UserId}: SaveChangesAsync returned {SavedChanges} entities updated",
                    request.ChannelId,
                    request.UserId,
                    savedChanges);

                _logger?.LogInformation(
                    "[FINAL] Channel {ChannelId} mark as read later toggled from {InitialState} to {NewState} for user {UserId}",
                    request.ChannelId,
                    initialState,
                    newState,
                    request.UserId);

                return Result.Success(member.IsMarkedReadLater);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error toggling mark as read later for channel {ChannelId} by user {UserId}",
                    request.ChannelId,
                    request.UserId);
                return Result.Failure<bool>(ex.Message);
            }
        }
    }
}