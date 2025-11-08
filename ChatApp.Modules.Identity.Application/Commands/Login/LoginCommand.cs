using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
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



    public class LoginCommandHandler:IRequestHandler<LoginCommand,Result<LoginResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly ILogger<LoginCommand> _logger;

        public LoginCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ITokenGenerator tokenGenerator,
            ILogger<LoginCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher= passwordHasher;
            _tokenGenerator= tokenGenerator;
            _logger= logger;
        }


        public async Task<Result<LoginResponse>> Handle(
            LoginCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Login attempt for username: {Username}", command.Username);

                var user = await _unitOfWork.Users.GetFirstOrDefaultAsync(
                    u => u.Username == command.Username,
                    cancellationToken);

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
                var permissions = await _unitOfWork.UserRoles.GetPermissionsByUserIdAsync(user.Id, cancellationToken);

                var permissionNames = permissions.Select(p => p.Name).ToList();

                // Generate tokens
                var accessToken = _tokenGenerator.GenerateAccessToken(user, permissionNames);
                var refreshToken = _tokenGenerator.GenerateRefreshToken();

                // Save refresh token
                var refreshTokenEntity = new Domain.Entities.RefreshToken(
                    user.Id,
                    refreshToken,
                    DateTime.UtcNow.AddDays(30));

                await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("Login successful for user {Username}", command.Username);

                return Result.Success(new LoginResponse
                (
                    accessToken,
                    refreshToken,
                    28800 //8 hours in seconds
                ));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during login for username: {Username}", command.Username);
                return Result.Failure<LoginResponse>("An error occured during login");
            }
        }
    }
}