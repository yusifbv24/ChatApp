using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetPermissions
{
    public record GetPermissionsQuery(
        string? Module
    ) : IRequest<Result<List<PermissionDto>>>;



    public class GetPermissionsQueryHandler:IRequestHandler<GetPermissionsQuery,Result<List<PermissionDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetPermissionsQuery> _logger;

        public GetPermissionsQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetPermissionsQuery> logger)
        {
            _unitOfWork= unitOfWork;
            _logger= logger;
        }

        public async Task<Result<List<PermissionDto>>> Handle(
            GetPermissionsQuery request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var permissions = string.IsNullOrWhiteSpace(request.Module)
                    ? await _unitOfWork.Permissions.GetAllAsync(cancellationToken)
                    : await _unitOfWork.Permissions.FindAsync(
                        p=>p.Module==request.Module,
                        cancellationToken);

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
                _logger?.LogError(ex, "Error retrieving permissions");
                return Result.Failure<List<PermissionDto>>("An error occurred while retrieving permissions");
            }
        }
    }
}