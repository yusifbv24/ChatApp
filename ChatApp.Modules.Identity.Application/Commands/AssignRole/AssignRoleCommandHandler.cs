using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.AssignRole
{
    public class AssignRoleCommandHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IEventBus _eventBus;
        private readonly ILogger<AssignRoleCommandHandler> _logger;

        public AssignRoleCommandHandler(
            IUserRepository userRepsitory,
            IRoleRepository roleRepository,
            IEventBus eventBus,
            ILogger<AssignRoleCommandHandler> logger)
        {
            _userRepository= userRepsitory;
            _roleRepository= roleRepository;
            _eventBus= eventBus;
            _logger= logger;
        }

        public async Task<Result> HandleAsync(
            AssignRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);

                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                    throw new NotFoundException($"User with ID {request.UserId} not found");

                var role=await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
                if (role == null)
                    throw new NotFoundException($"Role with ID {request.RoleId} not found");

                var userRole = new UserRole(
                    request.UserId,
                    request.RoleId,
                    request.AssignedBy);

                user.AssignRole(userRole);

                await _userRepository.UpdateAsync(user, cancellationToken);

                await _eventBus.PublishAsync(new RoleAssignedEvent(request.UserId,request.RoleId),cancellationToken);
                _logger?.LogInformation("Role {RoleId} assigned to user {UserId} successfully", request.RoleId, request.UserId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}