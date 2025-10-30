using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetPermissions
{
    public class GetPermissionsQueryHandler
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

        public async Task<Result<List<PermissionDto>>> HandleAsync(GetPermissionsQuery request,CancellationToken cancellationToken = default)
        {
            try
            {
                var permissions = string.IsNullOrWhiteSpace(request.Module)
                    ? await _permissionRepository.GetAllAsync(cancellationToken)
                    : await _permissionRepository.GetByModuleAsync(request.Module);

                var permissionDtos=permissions.Select(p=>new PermissionDto
                {
                    Id=p.Id,
                    Name=p.Name,
                    Description=p.Description,
                    Module=p.Module
                }).ToList();

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