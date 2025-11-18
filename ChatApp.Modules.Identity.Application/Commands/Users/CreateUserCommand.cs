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
        bool IsAdmin,
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




    public class CreateUserCommandHandler:IRequestHandler<CreateUserCommand, Result<Guid>>
    {
        private readonly Interfaces.IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEventBus _eventBus;
        private readonly ILogger<CreateUserCommand> _logger;

        public CreateUserCommandHandler(
            Interfaces.IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IEventBus eventBus,
            ILogger<CreateUserCommand> logger)
        {
            _unitOfWork= unitOfWork;
            _passwordHasher= passwordHasher;
            _eventBus= eventBus;
            _logger= logger;
        }


        public async Task<Result<Guid>> Handle(
            CreateUserCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Creating user : {Username} ", command.Username);
                if(await _unitOfWork.Users.AnyAsync(u=>u.Username==command.Username, cancellationToken))
                {
                    _logger?.LogWarning("Username {Username} already exists", command.Username);
                    return Result.Failure<Guid>("Username already exists");
                }
                
                if(await _unitOfWork.Users.AnyAsync(u=>u.Email==command.Email, cancellationToken))
                {
                    _logger?.LogWarning("Email {Email} already exists", command.Email);
                    return Result.Failure<Guid>("Email already exists");
                }
               
                var passwordHash=_passwordHasher.Hash(command.Password);
                var user = new User(
                    command.Username,
                    command.Email,
                    passwordHash,
                    command.DisplayName,
                    command.CreatedBy,
                    command.AvatarUrl,
                    command.Notes,
                    command.IsAdmin);
                await _unitOfWork.Users.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish event
                await _eventBus.PublishAsync(new UserCreatedEvent(user.Id, user.Username, user.DisplayName,user.CreatedBy));

                _logger?.LogInformation("User {Username} created succesfully with ID {UserId}", command.Username, user.Id);
                return Result.Success(user.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating user {Username}", command.Username);
                return Result.Failure<Guid>("An error occurred while creating the user");
            }
        }
    }
}