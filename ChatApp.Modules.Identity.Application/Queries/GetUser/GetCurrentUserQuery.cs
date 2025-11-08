using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetUser
{
    public record GetCurrentUserQuery(
        Guid UserId
    ):IRequest<Result<UserDto?>>;


    public class GetCurrentUserQueryHandler:IRequestHandler<GetCurrentUserQuery, Result<UserDto?>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetCurrentUserQueryHandler> _logger;
        public GetCurrentUserQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetCurrentUserQueryHandler> logger)
        {
            _unitOfWork=unitOfWork;
            _logger=logger;
        }

        public async Task<Result<UserDto?>> Handle(
            GetCurrentUserQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);

                if(user is null)
                {
                    _logger?.LogWarning("User {UserId} not found", request.UserId);
                    return Result.Success<UserDto?>(null);
                }

                var userDto = new UserDto(
                    user.Id,
                    user.Username,
                    user.Email,
                    user.DisplayName,
                    user.AvatarUrl,
                    user.Notes,
                    user.CreatedBy,
                    user.IsActive,
                    user.IsAdmin,
                    user.CreatedAtUtc);

                return Result.Success<UserDto?>(userDto);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving current user information for user {UserId}", request.UserId);
                return Result.Failure<UserDto?>("An error occurred while retrieving your profile information");
            }
        }
    }
}