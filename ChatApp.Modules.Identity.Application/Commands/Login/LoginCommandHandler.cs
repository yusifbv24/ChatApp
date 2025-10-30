using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Login
{
    public class LoginCommandHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            ITokenGenerator tokenGenerator,
            IRefreshTokenRepository refreshTokenRepository,
            IPermissionRepository permissionRepository,
            ILogger<LoginCommandHandler> logger)
        {
            _userRepository= userRepository;
            _passwordHasher= passwordHasher;
            _tokenGenerator= tokenGenerator;
            _refreshTokenRepository= refreshTokenRepository;
            _permissionRepository= permissionRepository;
            _logger= logger;
        }


        public async Task<Result<LoginResponse>> HandleAsync(
            LoginCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Login attempt for username: {Username}", command.Username);

                var user = await _userRepository.GetByUsernameAsync(command.Username, cancellationToken);

                if (user == null)
                {
                    _logger?.LogWarning("Login failed : User {Username} not found",command.Username);
                    return Result.Failure<LoginResponse>("Invalid username or password");
                }

                if (!user.IsActive)
                {
                    _logger?.LogWarning("Login failed : User {Username} is not active", command.Username);
                    return Result.Failure<LoginResponse>("Account is deactivated");
                }

                if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
                {
                    _logger?.LogWarning("Login failed : Invalid password or username {Username}", command.Username);
                    return Result.Failure<LoginResponse>("Invalid username or password");
                }

                // Get user permissions
                var permissions = await _permissionRepository.GetByUserIdAsync(user.Id, cancellationToken);
                var permissionNames = permissions.Select(p => p.Name).ToList();

                // Generate tokens
                var accessToken = _tokenGenerator.GenerateAccessToken(user, permissionNames);
                var refreshToken = _tokenGenerator.GenerateRefreshToken();

                // Save refresh token
                var refreshTokenEntity = new RefreshToken(
                    user.Id,
                    refreshToken,
                    DateTime.UtcNow.AddDays(30));

                await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

                _logger?.LogInformation("Login successful for user {Username}", command.Username);

                return Result.Success(new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 28800 //8 hours in seconds
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during login for username: {Username}", command.Username);
                return Result.Failure<LoginResponse>("An error occured during login");
            }
        }
    }
}