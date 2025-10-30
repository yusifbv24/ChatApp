using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.DeleteUser
{
    public class DeleteUserCommandHandler:IRequestHandler<DeleteUserCommand,Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEventBus _eventBus;
        private readonly ILogger<DeleteUserCommandHandler> _logger;
        public DeleteUserCommandHandler(
            IUserRepository userRepository,
            IEventBus eventBus,
            ILogger<DeleteUserCommandHandler> logger)
        {
            _userRepository=userRepository;
            _eventBus=eventBus;
            _logger=logger;
        }


        public async Task<Result> Handle(
            DeleteUserCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Deleting user {UserId}", request.UserId);

                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                    throw new NotFoundException($"User with ID {request.UserId} not found");

                await _userRepository.DeleteAsync(user, cancellationToken);

                await _eventBus.PublishAsync(new UserDeletedEvent(user.Id, user.Username), cancellationToken);

                _logger?.LogInformation("User {UserId} deleted successfully", request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}