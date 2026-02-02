using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Queries.GetUserChannels;

public record GetUserChannelsPagedQuery(
    Guid UserId,
    int PageNumber,
    int PageSize
) : IRequest<Result<PagedResult<ChannelDto>>>;

public class GetUserChannelsPagedQueryHandler(
    IUnitOfWork unitOfWork,
    ILogger<GetUserChannelsPagedQueryHandler> logger)
    : IRequestHandler<GetUserChannelsPagedQuery, Result<PagedResult<ChannelDto>>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PagedResult<ChannelDto>>> Handle(
        GetUserChannelsPagedQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var pageSize = Math.Min(request.PageSize, MaxPageSize);

            var result = await unitOfWork.Channels.GetUserChannelDtosPagedAsync(
                request.UserId,
                request.PageNumber,
                pageSize,
                cancellationToken);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paged channels for user {UserId}", request.UserId);
            return Result.Failure<PagedResult<ChannelDto>>("An error occurred while retrieving channels");
        }
    }
}
