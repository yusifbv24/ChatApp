using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMembers
{
    public record MarkAllChannelMessagesAsReadCommand(
        Guid ChannelId,
        Guid UserId
    ) : IRequest<Result<int>>; // Returns count of messages marked as read

    public class MarkAllChannelMessagesAsReadCommandValidator : AbstractValidator<MarkAllChannelMessagesAsReadCommand>
    {
        public MarkAllChannelMessagesAsReadCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class MarkAllChannelMessagesAsReadCommandHandler : IRequestHandler<MarkAllChannelMessagesAsReadCommand, Result<int>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MarkAllChannelMessagesAsReadCommandHandler> _logger;

        public MarkAllChannelMessagesAsReadCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<MarkAllChannelMessagesAsReadCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<int>> Handle(
            MarkAllChannelMessagesAsReadCommand request,
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
                    return Result.Failure<int>("User is not a member of this channel");

                _logger?.LogInformation(
                    "[BEFORE MARK ALL] Channel {ChannelId} User {UserId}: IsMarkedReadLater={IsMarkedReadLater}, LastReadLaterMessageId={LastReadLaterMessageId}",
                    request.ChannelId,
                    request.UserId,
                    member.IsMarkedReadLater,
                    member.LastReadLaterMessageId);

                // Mark all unread messages as read
                var markedCount = await _unitOfWork.ChannelMessages.MarkAllAsReadAsync(
                    request.ChannelId,
                    request.UserId,
                    cancellationToken);

                // Clear ALL read later flags (both conversation-level and message-level)
                member.UnmarkMessageAsLater();          // LastReadLaterMessageId = null
                member.UnmarkConversationAsReadLater(); // IsMarkedReadLater = false

                var savedChanges = await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "[AFTER MARK ALL] Channel {ChannelId} User {UserId}: Marked {MarkedCount} messages, SaveChangesAsync returned {SavedChanges} entities updated",
                    request.ChannelId,
                    request.UserId,
                    markedCount,
                    savedChanges);

                _logger?.LogInformation(
                    "Marked {MarkedCount} messages as read and cleared read later flags for channel {ChannelId} and user {UserId}",
                    markedCount,
                    request.ChannelId,
                    request.UserId);

                return Result.Success(markedCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error marking all messages as read for channel {ChannelId} and user {UserId}",
                    request.ChannelId,
                    request.UserId);
                return Result.Failure<int>(ex.Message);
            }
        }
    }
}