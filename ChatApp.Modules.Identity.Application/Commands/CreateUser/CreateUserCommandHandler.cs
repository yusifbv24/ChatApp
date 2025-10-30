using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.CreateUser
{
    public class CreateUserCommandHandler:IRequestHandler<CreateUserCommand, Result<Guid>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEventBus _eventBus;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IEventBus eventBus,
            ILogger<CreateUserCommandHandler> logger)
        {
            _userRepository= userRepository;
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
                if(await _userRepository.UsernameExistsAsync(command.Username, cancellationToken))
                {
                    _logger?.LogWarning("Username {Username} already exists", command.Username);
                    return Result.Failure<Guid>("Username already exists");
                }
                
                if(await _userRepository.EmailExistsAsync(command.Email, cancellationToken))
                {
                    _logger?.LogWarning("Email {Email} already exists", command.Email);
                    return Result.Failure<Guid>("Email already exists");
                }
               
                var passwordHash=_passwordHasher.Hash(command.Password);
                var user = new User(
                    command.Username,
                    command.Email,
                    passwordHash,
                    command.IsAdmin);
                await _userRepository.AddAsync(user, cancellationToken);

                // Publish event
                await _eventBus.PublishAsync(new UserCreatedEvent(user.Id, user.Username, user.Email));

                _logger?.LogInformation("User {Username} created succesfully with ID {UserId}", command.Username, user.Id);
                return Result.Success(user.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating user {Username}", command.Username);
                return Result.Failure<Guid>("An error occured while creating the user");
            }
        }
    }
}