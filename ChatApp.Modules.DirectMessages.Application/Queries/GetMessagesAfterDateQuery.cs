using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Queries
{
    public record GetMessagesAfterDateQuery(
        Guid ConversationId,
        DateTime AfterUtc,
        Guid RequestedBy,
        int Limit = 100
    ) : IRequest<Result<List<DirectMessageDto>>>;


    public class GetMessagesAfterDateQueryHandler : IRequestHandler<GetMessagesAfterDateQuery, Result<List<DirectMessageDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetMessagesAfterDateQueryHandler> _logger;

        public GetMessagesAfterDateQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetMessagesAfterDateQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<DirectMessageDto>>> Handle(
            GetMessagesAfterDateQuery request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verify conversation exists
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                {
                    return Result.Failure<List<DirectMessageDto>>("Conversation not found");
                }

                // Get messages after date
                var messages = await _unitOfWork.Messages.GetMessagesAfterDateAsync(
                    request.ConversationId,
                    request.AfterUtc,
                    request.Limit,
                    cancellationToken);

                return Result.Success(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages after date for conversation {ConversationId}", request.ConversationId);
                return Result.Failure<List<DirectMessageDto>>("An error occurred while retrieving messages");
            }
        }
    }
}
