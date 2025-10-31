using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;


namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record AssignRoleCommand(
        Guid UserId,
        Guid RoleId,
        Guid? AssignedBy
    ) : IRequest<Result>;



    public class AssignRoleCommandHandler:IRequestHandler<AssignRoleCommand,Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;
        private readonly ILogger<AssignRoleCommand> _logger;

        public AssignRoleCommandHandler(
            IUnitOfWork unitOfWork,
            IEventBus eventBus,
            ILogger<AssignRoleCommand> logger)
        {
            _unitOfWork=unitOfWork;
            _eventBus= eventBus;
            _logger= logger;
        }


        public async Task<Result> Handle(
            AssignRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);

                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                    throw new NotFoundException($"User with ID {request.UserId} not found");

                var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
                if (role == null)
                    throw new NotFoundException($"Role with ID {request.RoleId} not found");

                var userRole = new UserRole(
                    request.UserId,
                    request.RoleId,
                    request.AssignedBy);

                user.AssignRole(userRole);

                await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _eventBus.PublishAsync(new RoleAssignedEvent(request.UserId,request.RoleId),cancellationToken);
                _logger?.LogInformation("Role {RoleId} assigned to user {UserId} successfully", request.RoleId, request.UserId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}