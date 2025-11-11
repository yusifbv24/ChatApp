using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetUser
{
    public record GetUserQuery(
        Guid UserId
    ):IRequest<Result<UserDto?>>;


    public class GetUserQueryHandler : IRequestHandler<GetUserQuery, Result<UserDto?>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetUserQueryHandler> _logger;

        public GetUserQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetUserQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<Result<UserDto?>> Handle(
            GetUserQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _unitOfWork.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);

                if (user == null)
                    return Result.Success<UserDto?>(null);

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
                    user.CreatedAtUtc,
                    user.UserRoles.Select(ur=>new RoleDto(
                        ur.Role.Id,
                        ur.Role.Name,
                        ur.Role.Description,
                        ur.Role.IsSystemRole,
                        [],
                        0,
                        ur.Role.CreatedAtUtc
                    )).ToList()
                );

                return Result.Success<UserDto?>(userDto);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user {UserId}", query.UserId);
                return Result.Failure<UserDto?>("An error occurred while retrieving the user");
            }
        }
    }
}