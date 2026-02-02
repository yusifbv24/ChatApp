using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Queries;

public record GetConversationsPagedQuery(
    Guid UserId,
    int PageNumber,
    int PageSize
) : IRequest<Result<PagedResult<DirectConversationDto>>>;

public class GetConversationsPagedQueryHandler(
    IUnitOfWork unitOfWork,
    ILogger<GetConversationsPagedQueryHandler> logger)
    : IRequestHandler<GetConversationsPagedQuery, Result<PagedResult<DirectConversationDto>>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PagedResult<DirectConversationDto>>> Handle(
        GetConversationsPagedQuery request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pageSize = Math.Min(request.PageSize, MaxPageSize);

            var result = await unitOfWork.Conversations.GetUserConversationsPagedAsync(
                request.UserId,
                request.PageNumber,
                pageSize,
                cancellationToken);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paged conversations for user {UserId}", request.UserId);
            return Result.Failure<PagedResult<DirectConversationDto>>("An error occurred while retrieving conversations");
        }
    }
}