using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Enums;
using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record CreateUserCommand(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        Role Role,
        Guid? PositionId,
        string? AvatarUrl,
        string? AboutMe,
        DateTime? DateOfBirth,
        string? WorkPhone,
        DateTime? HiringDate
    ) : IRequest<Result<Guid>>;

    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name cannot be empty")
                .MinimumLength(2).WithMessage("First name must be at least 2 characters long")
                .MaximumLength(100).WithMessage("First name must be at most 100 characters long");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name cannot be empty")
                .MinimumLength(2).WithMessage("Last name must be at least 2 characters long")
                .MaximumLength(100).WithMessage("Last name must be at most 100 characters long");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email cannot be empty")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid role");

            When(x => !string.IsNullOrWhiteSpace(x.AboutMe), () =>
            {
                RuleFor(x => x.AboutMe)
                    .MaximumLength(2000).WithMessage("About me must not exceed 2000 characters");
            });

            When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl), () =>
            {
                RuleFor(x => x.AvatarUrl)
                    .MaximumLength(500).WithMessage("Avatar URL must not exceed 500 characters");
            });

            When(x => !string.IsNullOrWhiteSpace(x.WorkPhone), () =>
            {
                RuleFor(x => x.WorkPhone)
                    .MaximumLength(50).WithMessage("Work phone must not exceed 50 characters");
            });
        }
    }

    public class CreateUserCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEventBus eventBus,
        ILogger<CreateUserCommand> logger) : IRequestHandler<CreateUserCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(
            CreateUserCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                logger?.LogInformation("Creating user: {FirstName} {LastName} ({Email})",
                    command.FirstName, command.LastName, command.Email);

                // Check if email already exists
                if (await unitOfWork.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
                {
                    logger?.LogWarning("Email {Email} already exists", command.Email);
                    return Result.Failure<Guid>("Email already exists");
                }

                var passwordHash = passwordHasher.Hash(command.Password);

                // Create User (Authentication & Basic Profile)
                var user = new User(
                    command.FirstName,
                    command.LastName,
                    command.Email,
                    passwordHash,
                    command.Role,
                    command.AvatarUrl);

                await unitOfWork.Users.AddAsync(user, cancellationToken);

                // Create Employee (Organizational & Sensitive Data)
                // Every user must have an employee record (1:1 mandatory)
                var employee = new Employee(
                    user.Id,
                    command.DateOfBirth,
                    command.WorkPhone,
                    command.AboutMe,
                    command.HiringDate);

                if (command.PositionId.HasValue)
                {
                    employee.AssignToPosition(command.PositionId.Value);
                }

                await unitOfWork.Employees.AddAsync(employee, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish event
                await eventBus.PublishAsync(new UserCreatedEvent(
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Id)); // CreatedBy = self for now

                logger?.LogInformation("User {Email} created successfully with ID {UserId}", command.Email, user.Id);
                return Result.Success(user.Id);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error creating user {Email}", command.Email);
                return Result.Failure<Guid>("An error occurred while creating the user");
            }
        }
    }
}