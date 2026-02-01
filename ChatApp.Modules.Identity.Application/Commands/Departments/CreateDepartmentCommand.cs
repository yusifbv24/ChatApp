using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Departments
{
    public record CreateDepartmentCommand(
        string Name,
        Guid CompanyId,
        Guid? ParentDepartmentId
    ) : IRequest<Result<Guid>>;

    public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
    {
        public CreateDepartmentCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Department name is required")
                .MaximumLength(150).WithMessage("Department name must not exceed 150 characters");
        }
    }

    public class CreateDepartmentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateDepartmentCommandHandler> logger) : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(
            CreateDepartmentCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                // Validate parent department exists if provided
                if (command.ParentDepartmentId.HasValue)
                {
                    var parentExists = await unitOfWork.Departments
                        .AnyAsync(d => d.Id == command.ParentDepartmentId.Value, cancellationToken);

                    if (!parentExists)
                        return Result.Failure<Guid>("Parent department not found");
                }

                // Check for duplicate department name
                var isDuplicate = await unitOfWork.Departments
                    .AnyAsync(d => d.Name == command.Name, cancellationToken);

                if (isDuplicate)
                    return Result.Failure<Guid>("A department with this name already exists");

                // Validate company exists
                var companyExists = await unitOfWork.Companies
                    .AnyAsync(c => c.Id == command.CompanyId, cancellationToken);
                if (!companyExists)
                    return Result.Failure<Guid>("Company not found");

                Department department;
                if (command.ParentDepartmentId.HasValue)
                {
                    // Subdepartment
                    department = new Department(command.Name, command.CompanyId, command.ParentDepartmentId.Value);
                }
                else
                {
                    // Top-level department
                    department = new Department(command.Name, command.CompanyId);
                }

                await unitOfWork.Departments.AddAsync(department, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Department {DepartmentName} created with ID {DepartmentId}",
                    department.Name, department.Id);

                return Result.Success(department.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating department {DepartmentName}", command.Name);
                return Result.Failure<Guid>("An error occurred while creating the department");
            }
        }
    }
}