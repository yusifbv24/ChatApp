using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Queries
{
    public record GetConversationMessagesQuery(
        Guid ConversationId,
        Guid RequestedBy,
        int PageSize=30,
        DateTime? BeforeUtc=null
    ):IRequest<Result<List<DirectMessageDto>>>;


    public class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, Result<List<DirectMessageDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetConversationMessagesQueryHandler> _logger;

        public GetConversationMessagesQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetConversationMessagesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<DirectMessageDto>>> Handle(
            GetConversationMessagesQuery request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var conversations = await _unitOfWork.Conversations.GetByIdAsync(
                    request.ConversationId,
                    cancellationToken);

                if (conversations == null)
                {
                    return Result.Failure<List<DirectMessageDto>>("You are not a participant in this conversation");
                }

                var messages = await _unitOfWork.Messages.GetConversationMessagesAsync(
                    request.ConversationId,
                    request.PageSize,
                    request.BeforeUtc,
                    cancellationToken);

                return Result.Success(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages for conversation {ConversationId}", request.ConversationId);
                return Result.Failure<List<DirectMessageDto>>("An error occurred while retrieving messages");
            }
        }
    }
}