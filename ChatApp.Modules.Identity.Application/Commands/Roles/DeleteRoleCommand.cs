using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Roles
{
    public record DeleteRoleCommand(
        Guid RoleId
    ):IRequest<Result>;

    public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
    {
        public DeleteRoleCommandValidator()
        {
            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role ID is required");
        }
    }

    public class DeleteRoleCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteRoleCommandHandler> logger) : IRequestHandler<DeleteRoleCommand, Result>
    {
        public async Task<Result> Handle(
            DeleteRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var existingRole = await unitOfWork.Roles
                    .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

                if (existingRole == null)
                {
                    logger?.LogWarning("Role was not found to delete");
                    return Result.Failure("Role was not found to delete");
                }

                if (existingRole.IsSystemRole)
                {
                    logger?.LogWarning("Attempt to delete system role {RoleName}", existingRole.Name);
                    return Result.Failure("Cannot delete system role");
                }

                unitOfWork.Roles.Remove(existingRole);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger?.LogInformation("Role was removed succesfully with Name {RoleName}", existingRole.Name);
                return Result.Success("Role was removed succesfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error removing role {RoleId}", request.RoleId);
                return Result.Failure("An error occurred while removing the role");
            }
        }
    }
}