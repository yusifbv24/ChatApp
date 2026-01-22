using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetUser
{
    public record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserDetailDto?>>;

    public class GetCurrentUserQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetCurrentUserQueryHandler> logger) : IRequestHandler<GetCurrentUserQuery, Result<UserDetailDto?>>
    {
        public async Task<Result<UserDetailDto?>> Handle(
            GetCurrentUserQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var user = await unitOfWork.Users
                    .Include(u => u.UserPermissions)
                    .Include(u => u.Position)
                    .Include(u => u.Department)
                    .Include(u => u.Supervisor)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

                if (user is null)
                {
                    logger?.LogWarning("User {UserId} not found", request.UserId);
                    return Result.Success<UserDetailDto?>(null);
                }

                var userDto = MapToDetailDto(user);

                return Result.Success<UserDetailDto?>(userDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving current user information for user {UserId}", request.UserId);
                return Result.Failure<UserDetailDto?>("An error occurred while retrieving your profile information");
            }
        }

        private static UserDetailDto MapToDetailDto(Domain.Entities.User user)
        {
            var permissions = user.UserPermissions
                .Select(up => up.PermissionName)
                .ToList();

            return new UserDetailDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role.ToString(),
                user.Position?.Name,
                user.AvatarUrl,
                user.AboutMe,
                user.DateOfBirth,
                user.WorkPhone,
                user.HiringDate,
                user.LastVisit,
                user.IsActive,
                user.IsCEO,
                user.DepartmentId,
                user.Department?.Name,
                user.SupervisorId,
                user.Supervisor?.FullName,
                permissions,
                user.CreatedAtUtc,
                user.UpdatedAtUtc);
        }
    }
}
