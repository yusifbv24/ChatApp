using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;


namespace ChatApp.Modules.Identity.Application.Commands.Roles
{
    public record CreateRoleCommand(
        string Name,
        string Description
    ) : IRequest<Result<Guid>>;



    public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Role name is required")
                .MaximumLength(100).WithMessage("Role must not exceed 100 charcters");

            When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(500).WithMessage("Role description must not exceed 500 characters");
            });
        }
    }



    public class CreateRoleCommandHandler:IRequestHandler<CreateRoleCommand,Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateRoleCommand> _logger;

        public CreateRoleCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreateRoleCommand> logger)
        {
            _unitOfWork= unitOfWork;
            _logger= logger;
        }

        public async Task<Result<Guid>> Handle(
            CreateRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Creating role: {RoleName}", request.Name);

                var existingRole=await _unitOfWork.Roles.GetByNameAsync(request.Name,cancellationToken);
                if(existingRole!= null)
                {
                    _logger?.LogWarning("Role {RoleName} already exists", request.Name);
                    return Result.Failure<Guid>("Role with this name already exists");
                }

                var role = new Role(request.Name, request.Description);
                await _unitOfWork.Roles.AddAsync(role, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("Role {RoleName} created succesfully with ID {RoleId}", request.Name, role.Id);
                return Result.Success(role.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating role {RoleName}", request.Name);
                return Result.Failure<Guid>("An error occurred while creating the role");
            }
        }
    }
}