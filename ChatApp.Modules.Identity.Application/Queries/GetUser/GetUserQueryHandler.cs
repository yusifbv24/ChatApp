using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetUser
{
    public class GetUserQueryHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserQueryHandler> _logger;

        public GetUserQueryHandler(
            IUserRepository userRepository,
            ILogger<GetUserQueryHandler> logger)
        {
            _userRepository= userRepository;
            _logger= logger;
        }

        public async Task<Result<UserDto?>> HandleAsync(GetUserQuery query,CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
                if (user == null)
                    return Result.Success<UserDto?>(null);

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    IsAdmin = user.IsAdmin,
                    CreatedAtUtc = user.CreatedAtUtc
                };

                return Result.Success<UserDto?>(userDto);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user {UserId}", query.UserId);
                return Result.Failure<UserDto?>("An error occurred while retrieving the user");
            }
        }
    }
}