using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.UpdateUser
{
    public class UpdateUserCommandHandler:IRequestHandler<UpdateUserCommand,Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(
            IUserRepository userRepository,
            ILogger<UpdateUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result> Handle(
            UpdateUserCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Updating user {UserId}", request.UserId);

                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

                if (user == null)
                    throw new NotFoundException($"User with ID {request.UserId} not found");

                if(!string.IsNullOrWhiteSpace(request.Email))
                    user.UpdateEmail(request.Email);

                if (request.IsActive.HasValue)
                {
                    if (request.IsActive.Value)
                        user.Activate();
                    else
                        user.Deactivate();
                }

                await _userRepository.UpdateAsync(user, cancellationToken);

                _logger?.LogInformation("User {UserId} updated successfully", request.UserId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user {UserId}", request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}