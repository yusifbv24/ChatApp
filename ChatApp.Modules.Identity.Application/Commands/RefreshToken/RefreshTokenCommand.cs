using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.RefreshToken
{
    public record RefreshTokenCommand(
        string RefreshToken
    ) : IRequest<Result<RefreshTokenResponse>>;



    public class RefreshTokenCommandHandler:IRequestHandler<RefreshTokenCommand,Result<RefreshTokenResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly ILogger<RefreshTokenCommand> _logger;

        public RefreshTokenCommandHandler(
            IUnitOfWork unitOfWork,
            ITokenGenerator tokenGenerator,
            ILogger<RefreshTokenCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _tokenGenerator= tokenGenerator;
            _logger= logger;
        }

        public async Task<Result<RefreshTokenResponse>> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Refresh token request received");

                var refreshToken = await _unitOfWork.RefreshTokens.GetFirstOrDefaultAsync(
                    x=>x.Token==request.RefreshToken,
                    cancellationToken);

                if(refreshToken==null || !refreshToken.IsValid())
                {
                    _logger?.LogInformation("Invalid or expired refresh token");
                    return Result.Failure<RefreshTokenResponse>("Invalid or expired refresh token");
                }

                var user = await _unitOfWork.Users.GetByIdAsync(refreshToken.UserId, cancellationToken);
                if(user==null || !user.IsActive)
                {
                    _logger?.LogWarning("User not found or inactive for refresh token");
                    return Result.Failure<RefreshTokenResponse>("User not found or inactive");
                }

                // Get user permissions
                var permissions = await _unitOfWork.UserRoles.GetPermissionsByUserIdAsync(user.Id, cancellationToken);
                var permissionNames=permissions.Select(p=>p.Name).ToList();

                // Generate new tokens
                var newAccessToken = _tokenGenerator.GenerateAccessToken(user, permissionNames);
                var newRefreshToken = _tokenGenerator.GenerateRefreshToken();

                // Revoke old refresh token
                refreshToken.Revoke();
                await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken, cancellationToken);

                // Save new refresh token
                var newRefreshTokenEntity = new Domain.Entities.RefreshToken(
                    user.Id,
                    newRefreshToken,
                    DateTime.UtcNow.AddDays(30));

                await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("Tokens refreshed succesfully for user {UserId}", user.Id);

                return Result.Success(new RefreshTokenResponse
                (
                    newAccessToken,
                    newRefreshToken,
                    28800
               ));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during token refresh");
                return Result.Failure<RefreshTokenResponse>("An error occured during token refresh");
            }
        }
    }
}