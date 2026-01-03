using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Queries
{
    public record GetConversationMessagesAroundQuery(
        Guid ConversationId,
        Guid MessageId,
        Guid RequestedBy,
        int Count = 50
    ) : IRequest<Result<List<DirectMessageDto>>>;

    public class GetConversationMessagesAroundQueryHandler : IRequestHandler<GetConversationMessagesAroundQuery, Result<List<DirectMessageDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetConversationMessagesAroundQueryHandler> _logger;

        public GetConversationMessagesAroundQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetConversationMessagesAroundQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<DirectMessageDto>>> Handle(
            GetConversationMessagesAroundQuery request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var conversation = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversation == null)
                {
                    return Result.Failure<List<DirectMessageDto>>("Conversation not found");
                }

                // Verify user is a participant
                if (conversation.User1Id != request.RequestedBy && conversation.User2Id != request.RequestedBy)
                {
                    return Result.Failure<List<DirectMessageDto>>("You are not a participant in this conversation");
                }

                var messages = await _unitOfWork.Messages.GetMessagesAroundAsync(
                    request.ConversationId,
                    request.MessageId,
                    request.Count,
                    cancellationToken);

                if (messages.Count == 0)
                {
                    return Result.Failure<List<DirectMessageDto>>("Message not found");
                }

                return Result.Success(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages around {MessageId} for conversation {ConversationId}",
                    request.MessageId, request.ConversationId);
                return Result.Failure<List<DirectMessageDto>>("An error occurred while retrieving messages");
            }
        }
    }
}
