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



    public class AssignPermissionCommandHandler:IRequestHandler<AssignPermissionCommand,Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignPermissionCommandHandler> _logger;

        public AssignPermissionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<AssignPermissionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger=logger;
        }



        public async Task<Result> Handle(AssignPermissionCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Assigning permission {PermissionId} to role {RoleId}", request.PermissionId, request.RoleId);

                var role = await _unitOfWork.Roles
                    .FirstOrDefaultAsync(u=>u.Id==request.RoleId,cancellationToken);

                if (role == null)
                    throw new NotFoundException($"Role with ID {request.RoleId} not found");

                if (role.IsSystemRole)
                {
                    _logger?.LogWarning("Attempt to modify system role {RoleName}", role.Name);
                    return Result.Failure("Cannot modify system role permissions");
                }

                var permission=await _unitOfWork.Permissions
                    .FirstOrDefaultAsync(u => u.Id == request.PermissionId, cancellationToken);

                if (permission == null)
                    throw new NotFoundException($"Permission with ID {request.PermissionId} not found");

                var rolePermission = new RolePermission(
                    request.RoleId,
                    request.PermissionId);

                await _unitOfWork.RolePermissions.AddAsync(rolePermission, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("Permission {PermissionId} assigned to role {RoleId} successfully", request.PermissionId, request.RoleId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", request.PermissionId, request.RoleId);
                return Result.Failure(ex.Message);
            }
        }
    }
}