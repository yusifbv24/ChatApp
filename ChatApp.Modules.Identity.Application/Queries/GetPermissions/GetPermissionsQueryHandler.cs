using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetPermissions
{
    public class GetPermissionsQueryHandler:IRequestHandler<GetPermissionsQuery,Result<List<PermissionDto>>>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<GetPermissionsQueryHandler> _logger;

        public GetPermissionsQueryHandler(
            IPermissionRepository permissionRepository,
            ILogger<GetPermissionsQueryHandler> logger)
        {
            _permissionRepository= permissionRepository;
            _logger= logger;
        }

        public async Task<Result<List<PermissionDto>>> Handle(
            GetPermissionsQuery request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var permissions = string.IsNullOrWhiteSpace(request.Module)
                    ? await _permissionRepository.GetAllAsync(cancellationToken)
                    : await _permissionRepository.GetByModuleAsync(request.Module);

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