using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record AdminChangePasswordCommand(
        Guid UserId,
        string NewPassword,
        string ConfirmNewPassword
    ):IRequest<Result>;


    public class AdminChangePasswordComamndValidator : AbstractValidator<AdminChangePasswordCommand>
    {
        public AdminChangePasswordComamndValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required")
                .MinimumLength(8).WithMessage("New password must be at least 8 characters")
                .MaximumLength(100).WithMessage("New password must not exceed 100 characters")
                .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("New password must contain at least one number")
                .Matches(@"[\W_]").WithMessage("New password must contain at least one special character");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Password confirmation is required")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
        }
    }


    public class AdminChangePasswordCommandHandler : IRequestHandler<AdminChangePasswordCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEventBus _eventBus;
        private readonly ILogger<AdminChangePasswordCommandHandler> _logger;

        public AdminChangePasswordCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IEventBus eventBus,
            ILogger<AdminChangePasswordCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task<Result> Handle(
            AdminChangePasswordCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the user
                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);

                if (user == null)
                    throw new NotFoundException($"User with ID {request.UserId} not found");

                // Hash the new password
                var newPasswordHash = _passwordHasher.Hash(request.NewPassword);

                // Update the password
                user.ChangePassword(newPasswordHash);

                // Save changes
                await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish password changed event for potential notifications or security logging
                await _eventBus.PublishAsync(
                    new UserPasswordChangedEvent(user.Id),
                    cancellationToken);

                _logger?.LogInformation("Admin changed the password of the user {UserId} succesfully", request.UserId);
                return Result.Success();
            }
            catch (NotFoundException ex)
            {
                _logger?.LogError(ex, "User {UserId} not found", request.UserId);
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error changing password for user {UserId} by Admin", request.UserId);
                return Result.Failure("An error occurred while changing user password by Admin");
            }
        }
    }
}
