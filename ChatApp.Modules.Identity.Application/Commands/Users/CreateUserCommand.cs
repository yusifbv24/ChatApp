using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Entities;
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
        string Username,
        string Email,
        string Password,
        string DisplayName,
        Guid CreatedBy,
        List<Guid> RoleIds,
        string? AvatarUrl,
        string? Notes
    ) : IRequest<Result<Guid>>;




    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username cannot be empty")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long")
                .MaximumLength(50).WithMessage("Username must be at most 50 characters long")
                .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, underscores and hyphens");


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


            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .MinimumLength(2).WithMessage("Display name must be at least 2 characters")
                .MaximumLength(100).WithMessage("Display name must not exceed 100 characters");


            RuleFor(x => x.CreatedBy)
                .NotEmpty().WithMessage("CreatedBy is required");

            RuleFor(x => x.RoleIds)
                .NotEmpty().WithMessage("At least one role must be selected")
                .Must(roleIds => roleIds != null && roleIds.Count > 0).WithMessage("At least one role must be selected")
                .Must(roleIds =>
                {
                    var administratorRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                    if (roleIds.Contains(administratorRoleId))
                    {
                        return roleIds.Count == 1;
                    }
                    return true;
                }).WithMessage("Administrator role cannot be combined with other roles");

            When(x => !string.IsNullOrWhiteSpace(x.Notes), () =>
            {
                RuleFor(x => x.Notes)
                    .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");
            });


            When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl), () =>
            {
                RuleFor(x => x.AvatarUrl)
                    .MaximumLength(500).WithMessage("Avatar url must not exceed 500 characters");
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
                logger?.LogInformation("Creating user : {Username} ", command.Username);
                if(await unitOfWork.Users.AnyAsync(u=>u.Username==command.Username, cancellationToken))
                {
                    logger?.LogWarning("Username {Username} already exists", command.Username);
                    return Result.Failure<Guid>("Username already exists");
                }

                if(await unitOfWork.Users.AnyAsync(u=>u.Email==command.Email, cancellationToken))
                {
                    logger?.LogWarning("Email {Email} already exists", command.Email);
                    return Result.Failure<Guid>("Email already exists");
                }

                if(await unitOfWork.Users.AnyAsync(u=>u.DisplayName==command.DisplayName, cancellationToken))
                {
                    logger?.LogWarning("Display name {DisplayName} already exists", command.DisplayName);
                    return Result.Failure<Guid>("Display name already exists");
                }

                // Validate that all role IDs exist
                var existingRoles = await unitOfWork.Roles
                    .Where(r => command.RoleIds.Contains(r.Id))
                    .ToListAsync(cancellationToken);

                if (existingRoles.Count != command.RoleIds.Count)
                {
                    logger?.LogWarning("One or more role IDs do not exist");
                    return Result.Failure<Guid>("One or more selected roles do not exist");
                }

                var passwordHash=passwordHasher.Hash(command.Password);
                var user = new User(
                    command.Username,
                    command.Email,
                    passwordHash,
                    command.DisplayName,
                    command.CreatedBy,
                    command.AvatarUrl,
                    command.Notes);
                await unitOfWork.Users.AddAsync(user, cancellationToken);

                // Create UserRole entries for each selected role
                foreach (var roleId in command.RoleIds)
                {
                    var userRole = new UserRole(user.Id, roleId);
                    await unitOfWork.UserRoles.AddAsync(userRole, cancellationToken);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish event
                await eventBus.PublishAsync(new UserCreatedEvent(user.Id, user.Username, user.DisplayName,user.CreatedBy));

                logger?.LogInformation("User {Username} created succesfully with ID {UserId}", command.Username, user.Id);
                return Result.Success(user.Id);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error creating user {Username}", command.Username);
                return Result.Failure<Guid>("An error occurred while creating the user");
            }
        }
    }
}