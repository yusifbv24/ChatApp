using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record AssignRoleCommand(
        Guid UserId,
        Guid RoleId,
        Guid? AssignedBy
    ) : IRequest<Result>;



    public class AssignRoleCommandHandler(
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        ILogger<AssignRoleCommand> logger) : IRequestHandler<AssignRoleCommand,Result>
    {
        public async Task<Result> Handle(
            AssignRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                logger?.LogInformation("Assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);

                var user = await unitOfWork.Users
                    .FirstOrDefaultAsync(r=>r.Id==request.UserId, cancellationToken)
                        ?? throw new NotFoundException($"User with ID {request.UserId} not found");

                var role = await unitOfWork.Roles
                    .FirstOrDefaultAsync(r=>r.Id==request.RoleId, cancellationToken)
                        ?? throw new NotFoundException($"Role with ID {request.RoleId} not found");

                // Get user's current roles
                var currentUserRoles = await unitOfWork.UserRoles
                    .Where(ur => ur.UserId == request.UserId)
                    .ToListAsync(cancellationToken);

                var administratorRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

                // Administrator exclusivity validation
                if (request.RoleId == administratorRoleId)
                {
                    // If assigning Administrator role, user must not have other roles
                    if (currentUserRoles.Any())
                    {
                        logger?.LogWarning("Cannot assign Administrator role to user {UserId} who already has other roles", request.UserId);
                        return Result.Failure("Administrator role cannot be combined with other roles. Please remove existing roles first.");
                    }
                }
                else
                {
                    // If assigning any other role, user must not have Administrator role
                    if (currentUserRoles.Any(ur => ur.RoleId == administratorRoleId))
                    {
                        logger?.LogWarning("Cannot assign role {RoleId} to user {UserId} who has Administrator role", request.RoleId, request.UserId);
                        return Result.Failure("Cannot assign additional roles to a user with Administrator role. Please remove Administrator role first.");
                    }
                }

                var userRole = new UserRole(
                    request.UserId,
                    request.RoleId,
                    request.AssignedBy);

                user.AssignRole(userRole);

                await unitOfWork.UserRoles.AddAsync(userRole, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                await eventBus.PublishAsync(new RoleAssignedEvent(request.UserId,request.RoleId),cancellationToken);
                logger?.LogInformation("Role {RoleId} assigned to user {UserId} successfully", request.RoleId, request.UserId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}