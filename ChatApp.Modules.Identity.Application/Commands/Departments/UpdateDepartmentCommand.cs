using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Departments
{
    public record UpdateDepartmentCommand(
        Guid DepartmentId,
        string? Name,
        Guid? ParentDepartmentId
    ) : IRequest<Result>;

    public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
    {
        public UpdateDepartmentCommandValidator()
        {
            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("Department ID is required");

            When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
            {
                RuleFor(x => x.Name)
                    .MaximumLength(150).WithMessage("Department name must not exceed 150 characters");
            });
        }
    }

    public class UpdateDepartmentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateDepartmentCommandHandler> logger) : IRequestHandler<UpdateDepartmentCommand, Result>
    {
        public async Task<Result> Handle(
            UpdateDepartmentCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                var department = await unitOfWork.Departments
                    .FirstOrDefaultAsync(d => d.Id == command.DepartmentId, cancellationToken);

                if (department == null)
                    return Result.Failure("Department not found");

                // Validate parent department exists if being updated
                if (command.ParentDepartmentId.HasValue)
                {
                    // Cannot set parent to itself
                    if (command.ParentDepartmentId.Value == command.DepartmentId)
                        return Result.Failure("A department cannot be its own parent");

                    var parentExists = await unitOfWork.Departments
                        .AnyAsync(d => d.Id == command.ParentDepartmentId.Value, cancellationToken);

                    if (!parentExists)
                        return Result.Failure("Parent department not found");
                }

                // Check for duplicate name (excluding current department)
                if (!string.IsNullOrWhiteSpace(command.Name))
                {
                    var isDuplicate = await unitOfWork.Departments
                        .AnyAsync(d => d.Id != command.DepartmentId && d.Name == command.Name, cancellationToken);

                    if (isDuplicate)
                        return Result.Failure("A department with this name already exists");
                }

                // Update department name if provided
                if (!string.IsNullOrWhiteSpace(command.Name))
                {
                    department.UpdateName(command.Name);
                }

                // Update parent department if provided
                if (command.ParentDepartmentId.HasValue)
                {
                    department.ChangeParentDepartment(command.ParentDepartmentId.Value);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Department {DepartmentId} updated successfully", command.DepartmentId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating department {DepartmentId}", command.DepartmentId);
                return Result.Failure("An error occurred while updating the department");
            }
        }
    }
}
