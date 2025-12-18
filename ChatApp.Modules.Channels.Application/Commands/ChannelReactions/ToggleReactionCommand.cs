using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelReactions
{
    public record ToggleReactionCommand(
        Guid MessageId,
        Guid UserId,
        string Reaction
    ) : IRequest<Result<List<ChannelMessageReactionDto>>>;

    public class ToggleReactionCommandValidator : AbstractValidator<ToggleReactionCommand>
    {
        public ToggleReactionCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Reaction)
                .NotEmpty().WithMessage("Reaction cannot be empty")
                .MaximumLength(10).WithMessage("Reaction must be a single emoji");
        }
    }

    public class ToggleReactionCommandHandler : IRequestHandler<ToggleReactionCommand, Result<List<ChannelMessageReactionDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<ToggleReactionCommandHandler> _logger;

        public ToggleReactionCommandHandler(
            IUnitOfWork unitOfWork,
            ISignalRNotificationService signalRNotificationService,
            ILogger<ToggleReactionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signalRNotificationService = signalRNotificationService;
            _logger = logger;
        }

        public async Task<Result<List<ChannelMessageReactionDto>>> Handle(
            ToggleReactionCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Load message with reactions (AsNoTracking for validation only)
                var message = await _unitOfWork.ChannelMessages.GetByIdWithReactionsAsync(
                    request.MessageId,
                    cancellationToken) 
                    ?? throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Verify user is a member
                var isMember = await _unitOfWork.Channels.IsUserMemberAsync(
                    message.ChannelId,
                    request.UserId,
                    cancellationToken);

                if (!isMember)
                {
                    return Result.Failure<List<ChannelMessageReactionDto>>("You must be a member to react to messages");
                }

                // Use domain method for toggle logic (DDD principle - business logic in domain)
                // User can only have ONE reaction per message - old reactions are automatically removed
                var (wasAdded, addedReaction, removedReaction) = message.ToggleReaction(
                    request.UserId,
                    request.Reaction);

                if (removedReaction!=null)
                {
                    await _unitOfWork.ChannelMessageReactions.RemoveReactionAsync(removedReaction, cancellationToken);
                }

                // Add new reaction to database (if user selected different emoji)
                if (wasAdded && addedReaction != null)
                {
                    await _unitOfWork.ChannelMessageReactions.AddReactionAsync(addedReaction, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Get updated reactions for this message with user details
                var reactionsGrouped = await _unitOfWork.ChannelMessageReactions.GetMessageReactionsWithUserDetailsAsync(
                    request.MessageId,
                    cancellationToken);

                // Send real-time notification with updated reactions list (simplified)
                await _signalRNotificationService.NotifyChannelMessageReactionsUpdatedAsync(
                    message.ChannelId,
                    request.MessageId,
                    reactionsGrouped);

                return Result.Success(reactionsGrouped);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<List<ChannelMessageReactionDto>>(ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error toggling reaction on message {MessageId}",
                    request.MessageId);
                return Result.Failure<List<ChannelMessageReactionDto>>("An error occurred while toggling the reaction");
            }
        }
    }
}