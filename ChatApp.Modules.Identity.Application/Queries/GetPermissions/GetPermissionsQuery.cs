using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetPermissions
{
    public record GetPermissionsQuery(
        string? Module
    ) : IRequest<Result<List<PermissionDto>>>;



    public class GetPermissionsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPermissionsQuery> logger) : IRequestHandler<GetPermissionsQuery,Result<List<PermissionDto>>>
    {
        public async Task<Result<List<PermissionDto>>> Handle(
            GetPermissionsQuery request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var permissions = string.IsNullOrWhiteSpace(request.Module)
                    ? await unitOfWork.Permissions.AsNoTracking().ToListAsync(cancellationToken)
                    : await unitOfWork.Permissions
                        .Where(p=>p.Module==request.Module)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                var permissionDtos=permissions.Select(p=>new PermissionDto
                (
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Module
                )).ToList();

                return Result.Success(permissionDtos);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error retrieving permissions");
                return Result.Failure<List<PermissionDto>>("An error occurred while retrieving permissions");
            }
        }
    }
}