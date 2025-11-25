using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetUser
{
    public record GetCurrentUserQuery(
        Guid UserId
    ):IRequest<Result<UserDto?>>;


    public class GetCurrentUserQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetCurrentUserQueryHandler> logger) : IRequestHandler<GetCurrentUserQuery, Result<UserDto?>>
    {
        public async Task<Result<UserDto?>> Handle(
            GetCurrentUserQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var user = await unitOfWork.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

                if(user is null)
                {
                    logger?.LogWarning("User {UserId} not found", request.UserId);
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
                    user.CreatedAtUtc,
                    user.UserRoles.Select(ur=>new RoleDto(
                        ur.Role.Id,
                        ur.Role.Name,
                        ur.Role.Description,
                        ur.Role.IsSystemRole,
                        ur.Role.RolePermissions.Select(rp => new PermissionDto(
                            rp.Permission.Id,
                            rp.Permission.Name,
                            rp.Permission.Description,
                            rp.Permission.Module
                        )).ToList(),
                        0,
                        ur.Role.CreatedAtUtc
                    ))
                    .ToList()
                );

                return Result.Success<UserDto?>(userDto);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error retrieving current user information for user {UserId}", request.UserId);
                return Result.Failure<UserDto?>("An error occurred while retrieving your profile information");
            }
        }
    }
}