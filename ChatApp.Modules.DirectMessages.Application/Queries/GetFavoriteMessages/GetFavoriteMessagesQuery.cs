using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Queries.GetFavoriteMessages
{
    public record GetFavoriteMessagesQuery(
        Guid ConversationId,
        Guid UserId
    ) : IRequest<Result<List<FavoriteDirectMessageDto>>>;

    public class GetFavoriteMessagesQueryValidator : AbstractValidator<GetFavoriteMessagesQuery>
    {
        public GetFavoriteMessagesQueryValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("Conversation ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class GetFavoriteMessagesQueryHandler : IRequestHandler<GetFavoriteMessagesQuery, Result<List<FavoriteDirectMessageDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetFavoriteMessagesQueryHandler> _logger;

        public GetFavoriteMessagesQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetFavoriteMessagesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<FavoriteDirectMessageDto>>> Handle(
            GetFavoriteMessagesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Getting favorite messages for conversation {ConversationId}", request.ConversationId);

                // Check if user is part of the conversation
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                {
                    return Result.Failure<List<FavoriteDirectMessageDto>>("Conversation not found");
                }

                if (conversation.User1Id != request.UserId && conversation.User2Id != request.UserId)
                {
                    return Result.Failure<List<FavoriteDirectMessageDto>>("You are not part of this conversation");
                }

                var favoriteMessages = await _unitOfWork.Favorites.GetFavoriteMessagesAsync(
                    request.UserId,
                    request.ConversationId,
                    cancellationToken);

                _logger?.LogInformation("Retrieved {Count} favorite messages for conversation {ConversationId}",
                    favoriteMessages.Count, request.ConversationId);

                return Result<List<FavoriteDirectMessageDto>>.Success(favoriteMessages);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting favorite messages for conversation {ConversationId}", request.ConversationId);
                return Result.Failure<List<FavoriteDirectMessageDto>>("An error occurred while retrieving favorite messages");
            }
        }
    }
}
