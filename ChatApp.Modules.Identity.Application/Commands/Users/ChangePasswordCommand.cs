using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    /// <summary>
    /// Command to change the current user's password
    /// Requires the old password for security verification
    /// </summary>
    public record ChangePasswordCommand(
        Guid UserId,
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword
    ) : IRequest<Result>;



    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required");

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

            // Ensure new password is different from current password
            RuleFor(x => x)
                .Must(x => x.NewPassword != x.CurrentPassword)
                .WithMessage("New password must be different from current password");
        }
    }




    public class ChangePasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEventBus eventBus,
        ILogger<ChangePasswordCommandHandler> logger) : IRequestHandler<ChangePasswordCommand, Result>
    {
        public async Task<Result> Handle(
            ChangePasswordCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                logger?.LogInformation("User {UserId} attempting to change password", request.UserId);

                // Get the user
                var user = await unitOfWork.Users
                    .FirstOrDefaultAsync(r=>r.Id==request.UserId, cancellationToken) 
                        ?? throw new NotFoundException($"User with ID {request.UserId} not found");

                // Verify current password
                if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    logger?.LogWarning("User {UserId} provided incorrect current password", request.UserId);
                    return Result.Failure("Current password is incorrect");
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    logger?.LogWarning("Inactive user {UserId} attempted to change password", request.UserId);
                    return Result.Failure("Your account is deactivated. Please contact an administrator.");
                }

                // Hash the new password
                var newPasswordHash = passwordHasher.Hash(request.NewPassword);

                // Update the password
                user.ChangePassword(newPasswordHash);

                // Save changes
                unitOfWork.Users.Update(user);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish password changed event for potential notifications or security logging
                await eventBus.PublishAsync(
                    new UserPasswordChangedEvent(user.Id),
                    cancellationToken);

                logger?.LogInformation("User {UserId} successfully changed password", request.UserId);
                return Result.Success();
            }
            catch (NotFoundException ex)
            {
                logger?.LogError(ex, "User {UserId} not found", request.UserId);
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error changing password for user {UserId}", request.UserId);
                return Result.Failure("An error occurred while changing your password");
            }
        }
    }
}