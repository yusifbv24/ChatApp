using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Constants;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record RemovePermissionFromUserCommand(
        Guid UserId,
        string PermissionName
    ) : IRequest<Result>;

    public class RemovePermissionFromUserCommandValidator : AbstractValidator<RemovePermissionFromUserCommand>
    {
        public RemovePermissionFromUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.PermissionName)
                .NotEmpty().WithMessage("Permission name is required")
                .Must(BeValidPermission).WithMessage("Invalid permission name");
        }

        private bool BeValidPermission(string permissionName)
        {
            return Permissions.GetAll().Contains(permissionName);
        }
    }

    public class RemovePermissionFromUserCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RemovePermissionFromUserCommandHandler> logger) : IRequestHandler<RemovePermissionFromUserCommand, Result>
    {
        public async Task<Result> Handle(
            RemovePermissionFromUserCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                // Validate user exists
                var user = await unitOfWork.Users
                    .Include(u => u.UserPermissions)
                    .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

                if (user == null)
                    return Result.Failure("User not found");

                // Check if user has this permission
                if (!user.UserPermissions.Any(up => up.PermissionName == command.PermissionName))
                    return Result.Failure($"User does not have the permission '{command.PermissionName}'");

                // Remove permission
                user.RemovePermission(command.PermissionName);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Permission {PermissionName} removed from user {UserId}",
                    command.PermissionName,
                    command.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error removing permission {PermissionName} from user {UserId}",
                    command.PermissionName,
                    command.UserId);
                return Result.Failure("An error occurred while removing permission");
            }
        }
    }
}
