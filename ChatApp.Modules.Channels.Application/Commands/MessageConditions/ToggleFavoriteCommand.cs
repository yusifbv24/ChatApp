using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.MessageConditions
{
    public record ToggleFavoriteCommand(
        Guid ChannelId,
        Guid MessageId,
        Guid RequestedBy
    ) : IRequest<Result<bool>>; // Returns true if added, false if removed

    public class ToggleFavoriteCommandValidator : AbstractValidator<ToggleFavoriteCommand>
    {
        public ToggleFavoriteCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }

    public class ToggleFavoriteCommandHandler : IRequestHandler<ToggleFavoriteCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ToggleFavoriteCommandHandler> _logger;

        public ToggleFavoriteCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<ToggleFavoriteCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            ToggleFavoriteCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Verify message exists and belongs to the channel
                var message = await _unitOfWork.ChannelMessages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                if (message.ChannelId != request.ChannelId)
                    return Result.Failure<bool>("Message does not belong to the specified channel");

                if (message.IsDeleted)
                    return Result.Failure<bool>("Cannot add deleted messages to favorites");

                // Verify user is a member of the channel
                var member = await _unitOfWork.ChannelMembers.GetMemberAsync(
                    request.ChannelId,
                    request.RequestedBy,
                    cancellationToken);

                if (member == null)
                    return Result.Failure<bool>("User is not a member of this channel");

                // Check if already favorited
                var existingFavorite = await _unitOfWork.Favorites.GetAsync(
                    request.RequestedBy,
                    request.MessageId,
                    cancellationToken);

                if (existingFavorite != null)
                {
                    // Remove from favorites
                    await _unitOfWork.Favorites.RemoveAsync(existingFavorite, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger?.LogInformation(
                        "Message {MessageId} removed from favorites for user {UserId}",
                        request.MessageId,
                        request.RequestedBy);

                    return Result.Success(false); // Removed
                }
                else
                {
                    // Add to favorites
                    var favorite = new UserFavoriteChannelMessage(request.RequestedBy, request.MessageId);
                    await _unitOfWork.Favorites.AddAsync(favorite, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger?.LogInformation(
                        "Message {MessageId} added to favorites for user {UserId}",
                        request.MessageId,
                        request.RequestedBy);

                    return Result.Success(true); // Added
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error toggling favorite for message {MessageId} by user {UserId}",
                    request.MessageId,
                    request.RequestedBy);
                return Result.Failure<bool>(ex.Message);
            }
        }
    }
}
