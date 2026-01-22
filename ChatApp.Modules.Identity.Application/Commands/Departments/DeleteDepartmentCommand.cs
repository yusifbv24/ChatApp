using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Departments
{
    public record DeleteDepartmentCommand(Guid DepartmentId) : IRequest<Result>;

    public class DeleteDepartmentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteDepartmentCommandHandler> logger) : IRequestHandler<DeleteDepartmentCommand, Result>
    {
        public async Task<Result> Handle(
            DeleteDepartmentCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                var department = await unitOfWork.Departments
                    .Include(d => d.Positions)
                    .Include(d => d.Employees)
                    .Include(d => d.Subdepartments)
                    .FirstOrDefaultAsync(d => d.Id == command.DepartmentId, cancellationToken);

                if (department == null)
                    return Result.Failure("Department not found");

                // Check if department has subdepartments
                if (department.Subdepartments.Any())
                    return Result.Failure($"Cannot delete department. It has {department.Subdepartments.Count} subdepartment(s)");

                // Check if department has positions
                if (department.Positions.Any())
                    return Result.Failure($"Cannot delete department. It has {department.Positions.Count} position(s)");

                // Check if department has employees
                if (department.Employees.Any())
                    return Result.Failure($"Cannot delete department. It has {department.Employees.Count} employee(s) assigned");

                unitOfWork.Departments.Remove(department);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Department {DepartmentId} deleted successfully", command.DepartmentId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting department {DepartmentId}", command.DepartmentId);
                return Result.Failure("An error occurred while deleting the department");
            }
        }
    }
}
