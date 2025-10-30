using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetUser
{
    public class GetUserQueryHandler:IRequestHandler<GetUserQuery, Result<UserDto?>>
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

        public async Task<Result<UserDto?>> Handle(
            GetUserQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
                if (user == null)
                    return Result.Success<UserDto?>(null);

                var userDto = new UserDto
                (
                    user.Id,
                    user.Username,
                    user.Email,
                    user.IsActive,
                    user.IsAdmin,
                    user.CreatedAtUtc
                );

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