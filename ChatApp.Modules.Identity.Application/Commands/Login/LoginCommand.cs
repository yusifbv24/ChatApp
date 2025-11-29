using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Login
{
    public record LoginCommand(
        string Username,
        string Password
    ) : IRequest<Result<LoginResponse>>;


    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long")
                .MaximumLength(50).WithMessage("Username must be at most 50 characters long")
                .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters,numbers,underscores and hyphens");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
        }
    }



    public class LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        ILogger<LoginCommand> logger,
        IConfiguration configuration) : IRequestHandler<LoginCommand,Result<LoginResponse>>
    {
        public async Task<Result<LoginResponse>> Handle(
            LoginCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                logger?.LogInformation("Login attempt for username: {Username}", command.Username);

                var user = await unitOfWork.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Username == command.Username,cancellationToken);

                if (user == null)
                {
                    logger?.LogWarning("Login failed : User {Username} not found",command.Username);
                    return Result.Failure<LoginResponse>("Invalid username or password");
                }

                if (!user.IsActive)
                {
                    logger?.LogWarning("Login failed : User {Username} is not active", command.Username);
                    return Result.Failure<LoginResponse>("Account is deactivated");
                }

                if (!passwordHasher.Verify(command.Password, user.PasswordHash))
                {
                    logger?.LogWarning("Login failed : Invalid password or username {Username}", command.Username);
                    return Result.Failure<LoginResponse>("Invalid username or password");
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
                var accessTokenExpirationMinutes = int.TryParse(configuration["JwtSettings:AccessTokenExpirationMinutes"], out var accessMinutes) ? accessMinutes : 30;
                var refreshTokenExpirationDays = int.TryParse(configuration["JwtSettings:RefreshTokenExpirationDays"], out var refreshDays) ? refreshDays : 30;

                // Generate tokens
                var accessToken = tokenGenerator.GenerateAccessToken(user, permissions);
                var refreshToken = tokenGenerator.GenerateRefreshToken();

                // Save refresh token
                var refreshTokenEntity = new Domain.Entities.RefreshToken(
                    user.Id,
                    refreshToken,
                    DateTime.UtcNow.AddDays(refreshTokenExpirationDays));

                await unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger?.LogInformation("Login successful for user {Username}", command.Username);

                return Result.Success(new LoginResponse
                (
                    accessToken,
                    refreshToken,
                    accessTokenExpirationMinutes * 60 // Convert minutes to seconds
                ));
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during login for username: {Username}", command.Username);
                return Result.Failure<LoginResponse>("An error occurred during login");
            }
        }
    }
}