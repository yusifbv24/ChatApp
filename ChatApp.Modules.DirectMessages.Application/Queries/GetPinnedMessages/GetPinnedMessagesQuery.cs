using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Queries.GetPinnedMessages
{
    public record GetPinnedMessagesQuery(
        Guid ConversationId,
        Guid UserId
    ) : IRequest<Result<List<DirectMessageDto>>>;

    public class GetPinnedMessagesQueryValidator : AbstractValidator<GetPinnedMessagesQuery>
    {
        public GetPinnedMessagesQueryValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("Conversation ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class GetPinnedMessagesQueryHandler : IRequestHandler<GetPinnedMessagesQuery, Result<List<DirectMessageDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetPinnedMessagesQueryHandler> _logger;

        public GetPinnedMessagesQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetPinnedMessagesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<DirectMessageDto>>> Handle(
            GetPinnedMessagesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Getting pinned messages for conversation {ConversationId}", request.ConversationId);

                // Check if user is part of the conversation
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                {
                    return Result.Failure<List<DirectMessageDto>>("Conversation not found");
                }

                if (conversation.User1Id != request.UserId && conversation.User2Id != request.UserId)
                {
                    return Result.Failure<List<DirectMessageDto>>("You are not part of this conversation");
                }

                var pinnedMessages = await _unitOfWork.Messages.GetPinnedMessagesAsync(
                    request.ConversationId,
                    cancellationToken);

                _logger?.LogInformation("Retrieved {Count} pinned messages for conversation {ConversationId}",
                    pinnedMessages.Count, request.ConversationId);

                return Result<List<DirectMessageDto>>.Success(pinnedMessages);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting pinned messages for conversation {ConversationId}", request.ConversationId);
                return Result.Failure<List<DirectMessageDto>>("An error occurred while retrieving pinned messages");
            }
        }
    }
}
