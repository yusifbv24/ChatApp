using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            IRefreshTokenRepository refreshTokenRepository,
            IUserRepository userRepository,
            IPermissionRepository permissionRepository,
            ITokenGenerator tokenGenerator,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _refreshTokenRepository= refreshTokenRepository;
            _userRepository= userRepository;
            _permissionRepository= permissionRepository;
            _tokenGenerator= tokenGenerator;
            _logger= logger;
        }

        public async Task<Result<RefreshTokenResponse>> HandleAsync(
            RefreshTokenCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Refresh token request received");

                var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

                if(refreshToken==null || !refreshToken.IsValid())
                {
                    _logger?.LogInformation("Invalid or expired refresh token");
                    return Result.Failure<RefreshTokenResponse>("Invalid or expired refresh token");
                }

                var user = await _userRepository.GetByIdAsync(refreshToken.Id, cancellationToken);
                if(user==null || !user.IsActive)
                {
                    _logger?.LogWarning("User not found or inactive for refresh token");
                    return Result.Failure<RefreshTokenResponse>("User not found or inactive");
                }

                // Get user permissions
                var permissions = await _permissionRepository.GetByUserIdAsync(user.Id, cancellationToken);
                var permissionNames=permissions.Select(p=>p.Name).ToList();

                // Generate new tokens
                var newAccessToken = _tokenGenerator.GenerateAccessToken(user, permissionNames);
                var newRefreshToken = _tokenGenerator.GenerateRefreshToken();

                // Revoke old refresh token
                refreshToken.Revoke();
                await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

                // Save new refresh token
                var newRefreshTokenEntity = new Domain.Entities.RefreshToken(
                    user.Id,
                    newRefreshToken,
                    DateTime.UtcNow.AddDays(30));

                await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);
                _logger?.LogInformation("Tokens refreshed succesfully for user {UserId}", user.Id);

                return Result.Success(new RefreshTokenResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn = 28800
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during token refresh");
                return Result.Failure<RefreshTokenResponse>("An error occured during token refresh");
            }
        }
    }
}