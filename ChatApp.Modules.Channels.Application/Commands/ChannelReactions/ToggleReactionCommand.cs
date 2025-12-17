using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
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
                _logger?.LogInformation(
                    "Toggling reaction {Reaction} on message {MessageId}",
                    request.Reaction,
                    request.MessageId);

                var message = await _unitOfWork.ChannelMessages.GetByIdWithReactionsAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Verify user is a member
                var isMember = await _unitOfWork.Channels.IsUserMemberAsync(
                    message.ChannelId,
                    request.UserId,
                    cancellationToken);

                if (!isMember)
                {
                    return Result.Failure<List<ChannelMessageReactionDto>>("You must be a member to react to messages");
                }

                // Check if user has already reacted with this emoji
                var existingReaction = message.Reactions
                    .FirstOrDefault(r => r.UserId == request.UserId && r.Reaction == request.Reaction);

                bool wasAdded;
                if (existingReaction != null)
                {
                    // Remove the reaction
                    message.RemoveReaction(request.UserId, request.Reaction);
                    wasAdded = false;
                }
                else
                {
                    // Add the reaction
                    var reaction = new ChannelMessageReaction(
                        request.MessageId,
                        request.UserId,
                        request.Reaction);
                    message.AddReaction(reaction);
                    wasAdded = true;
                }

                await _unitOfWork.ChannelMessages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Get updated reactions grouped by emoji
                var reactionsGrouped = message.Reactions
                    .GroupBy(r => r.Reaction)
                    .Select(g => new ChannelMessageReactionDto(
                        g.Key,
                        g.Count(),
                        g.Select(r => r.UserId).ToList()
                    ))
                    .ToList();

                // Send real-time notification
                if (wasAdded)
                {
                    await _signalRNotificationService.NotifyReactionAddedAsync(
                        message.ChannelId,
                        request.MessageId,
                        request.UserId,
                        request.Reaction);
                }
                else
                {
                    await _signalRNotificationService.NotifyReactionRemovedAsync(
                        message.ChannelId,
                        request.MessageId,
                        request.UserId,
                        request.Reaction);
                }

                _logger?.LogInformation(
                    "Reaction {Reaction} {Action} for message {MessageId}",
                    request.Reaction,
                    wasAdded ? "added" : "removed",
                    request.MessageId);

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
