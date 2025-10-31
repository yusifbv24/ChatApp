using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
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

    public class DeleteRoleCommandHandler:IRequestHandler<DeleteRoleCommand, Result>
    {
        private readonly Interfaces.IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteRoleCommandHandler> _logger;

        public DeleteRoleCommandHandler(
            Interfaces.IUnitOfWork unitOfWork,
            ILogger<DeleteRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            DeleteRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var existingRole = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
                if (existingRole == null)
                {
                    _logger?.LogWarning("Role was not found to delete");
                    return Result.Failure("Role was not found to delete");
                }

                await _unitOfWork.Roles.DeleteAsync(existingRole,cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("Role was removed succesfully with Name {RoleName}", existingRole.Name);
                return Result.Success("Role was removed succesfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing role {RoleId}", request.RoleId);
                return Result.Failure("An error occurred while removing the role");
            }
        }
    }
}