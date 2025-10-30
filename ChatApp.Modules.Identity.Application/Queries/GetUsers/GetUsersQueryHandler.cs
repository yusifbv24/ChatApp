using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetUsers
{
    public class GetUsersQueryHandler:IRequestHandler<GetUsersQuery, Result<List<UserDto>>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUsersQueryHandler> _logger;

        public GetUsersQueryHandler(
            IUserRepository userRepository,
            ILogger<GetUsersQueryHandler> logger)
        {
            _userRepository= userRepository;
            _logger= logger;
        }

        public async Task<Result<List<UserDto>>> Handle(
            GetUsersQuery query, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await _userRepository.GetAllAsync(cancellationToken);

                var userDtos = users
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(u => new UserDto
                    (
                        u.Id,
                        u.Username,
                        u.Email,
                        u.IsActive,
                        u.IsAdmin,
                        u.CreatedAtUtc
                    ))
                    .ToList();

                return Result.Success(userDtos);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving users");
                return Result.Failure<List<UserDto>>("An error occured while retrieving users");
            }
        }
    }
}