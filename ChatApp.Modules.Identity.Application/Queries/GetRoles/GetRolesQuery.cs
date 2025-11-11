using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetRoles
{
    public record GetRolesQuery():IRequest<Result<List<RoleDto>>>;



    public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<List<RoleDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetRolesQueryHandler> _logger;

        public GetRolesQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetRolesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<Result<List<RoleDto>>> Handle(
            GetRolesQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var roles = await _unitOfWork.Roles
                    .Include(r=>r.RolePermissions)
                        .ThenInclude(rp=>rp.Permission)
                    .Include(r=>r.UserRoles)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var roleDtos = roles.Select(r => new RoleDto
                (
                    r.Id,
                    r.Name,
                    r.Description,
                    r.IsSystemRole,
                    r.RolePermissions.Select(rp=>new PermissionDto(
                        rp.Permission.Id,
                        rp.Permission.Name,
                        rp.Permission.Description,
                        rp.Permission.Module
                    )).ToList(),
                    r.UserRoles.Count,
                    r.CreatedAtUtc
                )).ToList();

                return Result.Success(roleDtos);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving roles");
                return Result.Failure<List<RoleDto>>("An error occurred while retrieving roles");
            }
        }
    }
}