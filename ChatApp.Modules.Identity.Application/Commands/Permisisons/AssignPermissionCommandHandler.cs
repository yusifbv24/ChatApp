using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Permisisons
{
    public record AssignPermissionCommand(
        Guid RoleId,
        Guid PermissionId
    ) : IRequest<Result>;



    public class AssignPermissionCommandValidator : AbstractValidator<AssignPermissionCommand>
    {
        public AssignPermissionCommandValidator()
        {
            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role ID is required");

            RuleFor(x => x.PermissionId)
                .NotEmpty().WithMessage("Permission ID is required");
        }
    }



    public class AssignPermissionCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AssignPermissionCommandHandler> logger) : IRequestHandler<AssignPermissionCommand,Result>
    {
        public async Task<Result> Handle(AssignPermissionCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                logger?.LogInformation("Assigning permission {PermissionId} to role {RoleId}", request.PermissionId, request.RoleId);

                var role = await unitOfWork.Roles
                    .FirstOrDefaultAsync(u=>u.Id==request.RoleId,cancellationToken) 
                        ?? throw new NotFoundException($"Role with ID {request.RoleId} not found");

                if (role.IsSystemRole)
                {
                    logger?.LogWarning("Attempt to modify system role {RoleName}", role.Name);
                    return Result.Failure("Cannot modify system role permissions");
                }

                var permission= await unitOfWork.Permissions
                    .FirstOrDefaultAsync(u => u.Id == request.PermissionId, cancellationToken) 
                        ?? throw new NotFoundException($"Permission with ID {request.PermissionId} not found");

                var rolePermission = new RolePermission(
                    request.RoleId,
                    request.PermissionId);

                await unitOfWork.RolePermissions.AddAsync(rolePermission, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger?.LogInformation("Permission {PermissionId} assigned to role {RoleId} successfully", request.PermissionId, request.RoleId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", request.PermissionId, request.RoleId);
                return Result.Failure(ex.Message);
            }
        }
    }
}