using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Queries
{
    public record GetConversationsQuery(
        Guid UserId
    ):IRequest<Result<List<DirectConversationDto>>>;


    public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, Result<List<DirectConversationDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetConversationsQueryHandler> _logger;

        public GetConversationsQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetConversationsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger= logger;
        }

        public async Task<Result<List<DirectConversationDto>>> Handle(
            GetConversationsQuery request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var conversations = await _unitOfWork.Conversations.GetUserConversationsAsync(
                    request.UserId,
                    cancellationToken);

                return Result.Success(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversations for user {UserId}", request.UserId);
                return Result.Failure<List<DirectConversationDto>>("An error occurred while retrieving conversations");
            }
        }
    }
}