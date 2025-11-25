using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetUsers
{
    public record GetUsersQuery(
        int PageNumber,
        int PageSize
    ):IRequest<Result<List<UserDto>>>;


    public class GetUsersQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUsersQueryHandler> logger) : IRequestHandler<GetUsersQuery, Result<List<UserDto>>>
    {
        public async Task<Result<List<UserDto>>> Handle(
            GetUsersQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await unitOfWork.Users
                    .Include(u=>u.UserRoles)
                        .ThenInclude(u=>u.Role)
                    .Skip((query.PageNumber-1)*query.PageSize)
                    .Take(query.PageSize)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var userDtos = users.Select(u => new UserDto(
                        u.Id,
                        u.Username,
                        u.Email,
                        u.DisplayName,
                        u.AvatarUrl,
                        u.Notes,
                        u.CreatedBy,
                        u.IsActive,
                        u.IsAdmin,
                        u.CreatedAtUtc,
                        u.UserRoles.Select(ur=>new RoleDto(
                            ur.Role.Id,
                            ur.Role.Name,
                            ur.Role.Description,
                            ur.Role.IsSystemRole,
                            [],
                            0,
                            ur.Role.CreatedAtUtc
                        )).ToList()
                    ))
                    .ToList();

                return Result.Success(userDtos);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error retrieving users");
                return Result.Failure<List<UserDto>>("An error occurred while retrieving users");
            }
        }
    }
}