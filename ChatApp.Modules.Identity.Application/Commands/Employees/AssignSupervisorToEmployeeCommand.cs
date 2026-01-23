using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Employees
{
    public record AssignSupervisorToEmployeeCommand(
        Guid UserId,
        Guid SupervisorId
    ) : IRequest<Result>;

    public class AssignSupervisorToEmployeeCommandValidator : AbstractValidator<AssignSupervisorToEmployeeCommand>
    {
        public AssignSupervisorToEmployeeCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.SupervisorId)
                .NotEmpty().WithMessage("Supervisor ID is required");

            RuleFor(x => x)
                .Must(x => x.UserId != x.SupervisorId)
                .WithMessage("User cannot be their own supervisor");
        }
    }

    public class AssignSupervisorToEmployeeCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AssignSupervisorToEmployeeCommandHandler> logger) : IRequestHandler<AssignSupervisorToEmployeeCommand, Result>
    {
        public async Task<Result> Handle(
            AssignSupervisorToEmployeeCommand command,
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

                // Validate supervisor exists and is active
                var supervisorUser = await unitOfWork.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.Id == command.SupervisorId, cancellationToken);

                if (supervisorUser == null)
                    return Result.Failure("Supervisor not found");

                if (supervisorUser.Employee == null)
                    return Result.Failure("Supervisor does not have an employee record");

                if (!supervisorUser.IsActive)
                    return Result.Failure("Cannot assign inactive user as supervisor");

                // Optional: Check if supervisor is in the same department
                if (user.Employee.DepartmentId.HasValue &&
                    supervisorUser.Employee.DepartmentId != user.Employee.DepartmentId)
                {
                    logger.LogWarning(
                        "Supervisor {SupervisorId} is in different department than employee {UserId}",
                        command.SupervisorId,
                        command.UserId);
                }

                // Assign supervisor
                user.Employee.AssignSupervisor(supervisorUser.Employee.Id);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Supervisor {SupervisorId} assigned to employee {UserId}",
                    command.SupervisorId,
                    command.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error assigning supervisor {SupervisorId} to employee {UserId}",
                    command.SupervisorId,
                    command.UserId);
                return Result.Failure("An error occurred while assigning supervisor");
            }
        }
    }
}