using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.CreateRole
{
    public class CreateRoleCommandHandler
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<CreateRoleCommandHandler> _logger;

        public CreateRoleCommandHandler(
            IRoleRepository roleRepository,
            ILogger<CreateRoleCommandHandler> logger)
        {
            _roleRepository= roleRepository;
            _logger= logger;
        }

        public async Task<Result<Guid>> HandleAsync(
            CreateRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Creating role: {RoleName}", request.Name);

                var existingRole=await _roleRepository.GetByNameAsync(request.Name,cancellationToken);
                if(existingRole!= null)
                {
                    _logger?.LogWarning("Role {RoleName} already exists", request.Name);
                    return Result.Failure<Guid>("Role with this name already exists");
                }

                var role = new Role(request.Name, request.Description);
                await _roleRepository.AddAsync(role, cancellationToken);

                _logger?.LogInformation("Role {RoleName} created succesfully with ID {RoleId}", request.Name, role.Id);
                return Result.Success(role.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating role {RoleName}", request.Name);
                return Result.Failure<Guid>("An error occurred while creating the role");
            }
        }
    }
}