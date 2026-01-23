using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Employees
{
    public record AssignEmployeeToDepartmentCommand(
        Guid UserId,
        Guid DepartmentId,
        Guid? SupervisorId = null,
        Guid? HeadOfDepartmentId = null
    ) : IRequest<Result>;

    public class AssignEmployeeToDepartmentCommandValidator : AbstractValidator<AssignEmployeeToDepartmentCommand>
    {
        public AssignEmployeeToDepartmentCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("Department ID is required");
        }
    }

    public class AssignEmployeeToDepartmentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AssignEmployeeToDepartmentCommandHandler> logger) : IRequestHandler<AssignEmployeeToDepartmentCommand, Result>
    {
        public async Task<Result> Handle(
            AssignEmployeeToDepartmentCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                // Validate user exists and has employee record
                var user = await unitOfWork.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

                if (user == null)
                    return Result.Failure("User not found");

                if (user.Employee == null)
                    return Result.Failure("User does not have an employee record");

                // Validate department exists
                var department = await unitOfWork.Departments
                    .FirstOrDefaultAsync(d => d.Id == command.DepartmentId, cancellationToken);

                if (department == null)
                    return Result.Failure("Department not found");

                // Validate supervisor if provided
                if (command.SupervisorId.HasValue)
                {
                    var supervisor = await unitOfWork.Employees
                        .FirstOrDefaultAsync(e => e.Id == command.SupervisorId.Value, cancellationToken);

                    if (supervisor == null)
                        return Result.Failure("Supervisor not found");

                    // Check if supervisor is in the same department
                    if (supervisor.DepartmentId != command.DepartmentId)
                        return Result.Failure("Supervisor must be in the same department");
                }

                // Validate head of department if provided
                if (command.HeadOfDepartmentId.HasValue)
                {
                    var headOfDepartment = await unitOfWork.Users
                        .FirstOrDefaultAsync(u => u.Id == command.HeadOfDepartmentId.Value, cancellationToken);

                    if (headOfDepartment == null)
                        return Result.Failure("Head of department not found");
                }

                // Assign to department
                user.Employee.AssignToDepartment(
                    command.DepartmentId,
                    command.SupervisorId,
                    command.HeadOfDepartmentId);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger?.LogInformation(
                    "Employee {UserId} assigned to department {DepartmentId}",
                    command.UserId,
                    command.DepartmentId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger?.LogError(
                    ex,
                    "Error assigning employee {UserId} to department {DepartmentId}",
                    command.UserId,
                    command.DepartmentId);
                return Result.Failure("An error occurred while assigning employee to department");
            }
        }
    }
}