using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Employees
{
    public record RemoveSupervisorFromEmployeeCommand(Guid UserId) : IRequest<Result>;

    public class RemoveSupervisorFromEmployeeCommandValidator : AbstractValidator<RemoveSupervisorFromEmployeeCommand>
    {
        public RemoveSupervisorFromEmployeeCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class RemoveSupervisorFromEmployeeCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RemoveSupervisorFromEmployeeCommandHandler> logger) : IRequestHandler<RemoveSupervisorFromEmployeeCommand, Result>
    {
        public async Task<Result> Handle(
            RemoveSupervisorFromEmployeeCommand command,
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

                if (user.Employee.SupervisorId == null)
                    return Result.Failure("Employee does not have a supervisor assigned");

                // Remove supervisor
                user.Employee.RemoveSupervisor();
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Supervisor removed from employee {UserId}",
                    command.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error removing supervisor from employee {UserId}",
                    command.UserId);
                return Result.Failure("An error occurred while removing supervisor");
            }
        }
    }
}