using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Queries.GetChannelMembers
{
    public record GetChannelMembersQuery(
        Guid ChannelId,
        Guid RequestedBy
    ) : IRequest<Result<List<ChannelMemberDto>>>;

    public class GetChannelMembersQueryHandler : IRequestHandler<GetChannelMembersQuery, Result<List<ChannelMemberDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetChannelMembersQueryHandler> _logger;

        public GetChannelMembersQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetChannelMembersQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<ChannelMemberDto>>> Handle(
            GetChannelMembersQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var channel = await _unitOfWork.Channels.GetByIdAsync(
                    request.ChannelId,
                    cancellationToken);

                if (channel == null)
                {
                    return Result.Failure<List<ChannelMemberDto>>("Channel not found");
                }

                // For private channels, verify user is a member
                if (channel.Type == Domain.Enums.ChannelType.Private)
                {
                    var isMember = await _unitOfWork.Channels.IsUserMemberAsync(
                        request.ChannelId,
                        request.RequestedBy,
                        cancellationToken);

                    if (!isMember)
                    {
                        return Result.Failure<List<ChannelMemberDto>>("You don't have access to this private channel");
                    }
                }

                // Repository handles the database join
                var memberDtos = await _unitOfWork.ChannelMembers.GetChannelMembersWithUserDataAsync(
                    request.ChannelId,
                    cancellationToken);

                return Result.Success(memberDtos);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving members for channel {ChannelId}", request.ChannelId);
                return Result.Failure<List<ChannelMemberDto>>("An error occurred while retrieving channel members");
            }
        }
    }
}