using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Roles
{
    public record UpdateRoleCommand(
        Guid RoleId,
        string? Name,
        string? Description
    ):IRequest<Result>;


    public class UpdateRoleCommandValidator: AbstractValidator<UpdateRoleCommand>
    {
        public UpdateRoleCommandValidator()
        {
            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role ID is required");

            When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Role name is required")
                    .MaximumLength(100).WithMessage("Role must not exceed 100 charcters");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(500).WithMessage("Role description must not exceed 500 characters");
            });
        }
    }


    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateRoleCommandHandler> _logger;

        public UpdateRoleCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateRoleCommandHandler> logger)
        {
            _unitOfWork= unitOfWork;
            _logger= logger;
        }


        public async Task<Result> Handle(
            UpdateRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Updating role: {RoleName}", request.Name);

                var existingRole=await _unitOfWork.Roles
                    .FirstOrDefaultAsync(r=>r.Id==request.RoleId,cancellationToken);

                if(existingRole == null)
                {
                    return Result.Failure("Role was not found");
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    var existingRoleName = await _unitOfWork.Roles
                        .Where(r=>r.Name==request.Name)
                        .ToListAsync(cancellationToken);

                    if(existingRoleName != null)
                    {
                        _logger?.LogWarning("Role with Name : {RoleName} already exists", request.Name);
                        return Result.Failure($"Role name already exists with {request.Name}");
                    }
                    existingRole.UpdateName(request.Name);
                }

                if (!string.IsNullOrWhiteSpace(request.Description))
                    existingRole.UpdateDescription(request.Description);

                _unitOfWork.Roles.Update(existingRole);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("Role {RoleName} updated succesfully with ID {RoleId}", request.Name, request.RoleId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating role {RoleName} ", request.Name);
                return Result.Failure("An error occurred while updating the role");
            }
        }
    }
}