using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Departments
{
    public record AssignDepartmentHeadCommand(
        Guid DepartmentId,
        Guid UserId
    ) : IRequest<Result>;

    public class AssignDepartmentHeadCommandValidator : AbstractValidator<AssignDepartmentHeadCommand>
    {
        public AssignDepartmentHeadCommandValidator()
        {
            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("Department ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class AssignDepartmentHeadCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AssignDepartmentHeadCommandHandler> logger) : IRequestHandler<AssignDepartmentHeadCommand, Result>
    {
        public async Task<Result> Handle(
            AssignDepartmentHeadCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                // Validate department exists
                var department = await unitOfWork.Departments
                    .FirstOrDefaultAsync(d => d.Id == command.DepartmentId, cancellationToken);

                if (department == null)
                    return Result.Failure("Department not found");

                // Validate user exists and is active
                var user = await unitOfWork.Users
                    .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

                if (user == null)
                    return Result.Failure("User not found");

                if (!user.IsActive)
                    return Result.Failure("Cannot assign inactive user as department head");

                // Assign head
                department.AssignHead(command.UserId);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "User {UserId} assigned as head of department {DepartmentId}",
                    command.UserId,
                    command.DepartmentId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error assigning user {UserId} as head of department {DepartmentId}",
                    command.UserId,
                    command.DepartmentId);
                return Result.Failure("An error occurred while assigning department head");
            }
        }
    }
}
