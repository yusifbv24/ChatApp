using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.AssignPermission
{
    public class AssignPermissionCommandHandler:IRequestHandler<AssignPermissionCommand,Result>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<AssignPermissionCommandHandler> _logger;

        public AssignPermissionCommandHandler(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            ILogger<AssignPermissionCommandHandler> logger)
        {
            _roleRepository=roleRepository;
            _permissionRepository=permissionRepository;
            _logger=logger;
        }


        public async Task<Result> Handle(AssignPermissionCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Assigning permission {PermissionId} to role {RoleId}", request.PermissionId, request.RoleId);

                var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
                if (role == null)
                    throw new NotFoundException($"Role with ID {request.RoleId} not found");

                var permission=await _permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
                if (permission == null)
                    throw new NotFoundException($"Permission with ID {request.PermissionId} not found");

                var rolePermission = new RolePermission(
                    request.RoleId,
                    request.PermissionId);
                role.AddPermission(rolePermission);

                await _roleRepository.UpdateAsync(role, cancellationToken);

                _logger?.LogInformation("Permission {PermissionId} assigned to role {RoleId} successfully", request.PermissionId, request.RoleId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", request.PermissionId, request.RoleId);
                return Result.Failure(ex.Message);
            }
        }
    }
}