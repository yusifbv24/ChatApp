using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.MessageConditions
{
    public record ToggleFavoriteCommand(
        Guid ConversationId,
        Guid MessageId,
        Guid RequestedBy
    ) : IRequest<Result<bool>>; // Returns true if added, false if removed

    public class ToggleFavoriteCommandValidator : AbstractValidator<ToggleFavoriteCommand>
    {
        public ToggleFavoriteCommandValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("Conversation ID is required");

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
                // Verify message exists and belongs to the conversation
                var message = await _unitOfWork.Messages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                if (message.ConversationId != request.ConversationId)
                    return Result.Failure<bool>("Message does not belong to the specified conversation");

                if (message.IsDeleted)
                    return Result.Failure<bool>("Cannot add deleted messages to favorites");

                // Verify user is a participant in the conversation
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                    throw new NotFoundException($"Conversation with ID {request.ConversationId} not found");

                if (!conversation.IsParticipant(request.RequestedBy))
                    return Result.Failure<bool>("User is not a participant in this conversation");

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
                    var favorite = new UserFavoriteMessage(request.RequestedBy, request.MessageId);
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
