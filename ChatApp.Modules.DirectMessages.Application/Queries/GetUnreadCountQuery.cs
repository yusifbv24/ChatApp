using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Queries
{
    public record GetUnreadCountQuery(
        Guid ConversationId,
        Guid UserId
    ):IRequest<Result<int>>;


    public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetUnreadCountQueryHandler> _logger;

        public GetUnreadCountQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetUnreadCountQueryHandler> logger)
        {
            _unitOfWork=unitOfWork;
            _logger=logger;
        }


        public async Task<Result<int>> Handle(
            GetUnreadCountQuery request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verify user is participant
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if(conversation == null || !conversation.IsParticipant(request.UserId))
                {
                    return Result.Failure<int>("You are not a participant in this conversation");
                }

                var unreadCount = await _unitOfWork.Messages.GetUnreadCountAsync(
                    request.ConversationId,
                    request.UserId,
                    cancellationToken);

                return Result.Success(unreadCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving unread count for conversation {ConversationId} and user {UserId}",
                    request.ConversationId,
                    request.UserId);
                return Result.Failure<int>("An error occurred while retrieving unread count");
            }
        }
    }
}