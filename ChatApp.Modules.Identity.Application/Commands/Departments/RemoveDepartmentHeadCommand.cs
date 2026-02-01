using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Departments
{
    public record RemoveDepartmentHeadCommand(Guid DepartmentId) : IRequest<Result>;

    public class RemoveDepartmentHeadCommandValidator : AbstractValidator<RemoveDepartmentHeadCommand>
    {
        public RemoveDepartmentHeadCommandValidator()
        {
            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("Department ID is required");
        }
    }

    public class RemoveDepartmentHeadCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RemoveDepartmentHeadCommandHandler> logger) : IRequestHandler<RemoveDepartmentHeadCommand, Result>
    {
        public async Task<Result> Handle(
            RemoveDepartmentHeadCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                var department = await unitOfWork.Departments
                    .FirstOrDefaultAsync(d => d.Id == command.DepartmentId, cancellationToken);

                if (department == null)
                    return Result.Failure("Department not found");

                if (department.HeadOfDepartmentId == null)
                    return Result.Failure("Department does not have a head assigned");

                var previousHeadUserId = department.HeadOfDepartmentId.Value;

                // === Auto Supervisor Cleanup ===

                // 1. Remove supervisor from all department members who had the old head as supervisor
                var oldHeadEmployee = await unitOfWork.Employees
                    .FirstOrDefaultAsync(e => e.UserId == previousHeadUserId, cancellationToken);

                if (oldHeadEmployee != null)
                {
                    var subordinates = await unitOfWork.Employees
                        .Where(e => e.SupervisorId == oldHeadEmployee.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var sub in subordinates)
                    {
                        sub.RemoveSupervisor();
                    }

                    // 2. Remove supervisor from the old head itself
                    oldHeadEmployee.RemoveSupervisor();
                }

                // Remove head from department
                department.RemoveHead();
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Head removed from department {DepartmentId}, supervisors cleared",
                    command.DepartmentId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error removing head from department {DepartmentId}",
                    command.DepartmentId);
                return Result.Failure("An error occurred while removing department head");
            }
        }
    }
}