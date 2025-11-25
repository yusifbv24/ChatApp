using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.RefreshToken
{
    public record RefreshTokenCommand(
        string RefreshToken
    ) : IRequest<Result<RefreshTokenResponse>>;



    public class RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        ITokenGenerator tokenGenerator,
        ILogger<RefreshTokenCommand> logger,
        IConfiguration configuration) : IRequestHandler<RefreshTokenCommand,Result<RefreshTokenResponse>>
    {
        public async Task<Result<RefreshTokenResponse>> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                logger?.LogInformation("Refresh token request received");

                var refreshToken = await unitOfWork.RefreshTokens
                   .FirstOrDefaultAsync(
                    x=>x.Token==request.RefreshToken,
                    cancellationToken);

                if(refreshToken==null || !refreshToken.IsValid())
                {
                    logger?.LogInformation("Invalid or expired refresh token");
                    return Result.Failure<RefreshTokenResponse>("Invalid or expired refresh token");
                }

                var user = await unitOfWork.Users
                    .FirstOrDefaultAsync(r => r.Id == refreshToken.UserId, cancellationToken);

                if(user==null || !user.IsActive)
                {
                    logger?.LogWarning("User not found or inactive for refresh token");
                    return Result.Failure<RefreshTokenResponse>("User not found or inactive");
                }

                // Get ONLY role-based permissions (no more direct user permissions)
                var permissions = await unitOfWork.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Include(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name))
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Get token expiration settings from configuration
                var accessTokenExpirationMinutes = configuration.GetValue<int>("JwtSettings:AccessTokenExpirationMinutes", 30);

                // Generate new tokens
                var newAccessToken = tokenGenerator.GenerateAccessToken(user, permissions);
                var newRefreshToken = tokenGenerator.GenerateRefreshToken();

                // Revoke old refresh token
                refreshToken.Revoke();
                unitOfWork.RefreshTokens.Update(refreshToken);

                // Save new refresh token with the same expiration as the old one
                var newRefreshTokenEntity = new Domain.Entities.RefreshToken(
                    user.Id,
                    newRefreshToken,
                    refreshToken.ExpiresAtUtc);

                await unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger?.LogInformation("Tokens refreshed succesfully for user {UserId}", user.Id);

                return Result.Success(new RefreshTokenResponse
                (
                    newAccessToken,
                    newRefreshToken,
                    accessTokenExpirationMinutes * 60 // Convert minutes to seconds
               ));
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during token refresh");
                return Result.Failure<RefreshTokenResponse>("An error occurred during token refresh");
            }
        }
    }
}