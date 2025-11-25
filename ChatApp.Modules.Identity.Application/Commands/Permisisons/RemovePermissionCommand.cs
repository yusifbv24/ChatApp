using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Permisisons
{
    public record RemovePermissionCommand(
        Guid RoleId,
        Guid PermissionId
    ):IRequest<Result>;



    public class RemovePermissionCommandValidator : AbstractValidator<RemovePermissionCommand>
    {
        public RemovePermissionCommandValidator()
        {
            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role ID is required");

            RuleFor(x => x.PermissionId)
                .NotEmpty().WithMessage("Permission ID is required");
        }
    }



    public class RemovePermissionCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RemovePermissionCommandHandler> logger) : IRequestHandler<RemovePermissionCommand, Result>
    {
        public async Task<Result> Handle(
            RemovePermissionCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                logger?.LogInformation("Removing permission {PermissionId} from role {RoleId}", request.PermissionId, request.RoleId);

                var role = await unitOfWork.Roles
                    .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken) 
                        ?? throw new NotFoundException($"Role with ID {request.RoleId} not found");

                if (role.IsSystemRole)
                {
                    logger?.LogWarning("Attempt to modify system role {RoleName}", role.Name);
                    return Result.Failure("Cannot modify system role permissions");
                }

                var rolePermission = await unitOfWork.RolePermissions
                   .FirstOrDefaultAsync(
                    r=>r.RoleId == request.RoleId
                    && r.PermissionId == request.PermissionId,
                    cancellationToken)
                        ?? throw new NotFoundException($"Permission {request.PermissionId} with this Role {request.RoleId} not found");

                unitOfWork.RolePermissions.Remove(rolePermission);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger?.LogInformation("Permission {PermissionId} removed from role {RoleId} successfully", request.PermissionId, request.RoleId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}", request.PermissionId, request.RoleId);
                return Result.Failure(ex.Message);
            }
        }
    }
}