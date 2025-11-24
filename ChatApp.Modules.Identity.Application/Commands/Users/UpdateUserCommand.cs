using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record UpdateUserCommand(
        Guid UserId,
        string? Email,
        string? DisplayName,
        string? Notes,
        string? AvatarUrl
    ) : IRequest<Result>;



    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required to update user");

            When(x => !string.IsNullOrWhiteSpace(x.DisplayName), () =>
            {
                RuleFor(x => x.DisplayName)
                    .MinimumLength(2).WithMessage("Display name must be at least 2 characters")
                    .MaximumLength(100).WithMessage("Display name must not exceed 100 chacters");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required")
                    .MaximumLength(255).WithMessage("Email must not exceed 255 characters")
                    .EmailAddress().WithMessage("Invalid email format");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Notes), () =>
            {
                RuleFor(x => x.Notes)
                    .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");
            });
        }
    }




    public class UpdateUserCommandHandler:IRequestHandler<UpdateUserCommand,Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateUserCommand> _logger;

        public UpdateUserCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateUserCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            UpdateUserCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Updating user {UserId}", request.UserId);

                var user = await _unitOfWork.Users
                    .FirstOrDefaultAsync(r=>r.Id==request.UserId, cancellationToken);

                if (user == null)
                    throw new NotFoundException($"User with ID {request.UserId} not found");


                // Check if user is trying to update email and if it's already taken by another user
                if(!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    var emailExists= await _unitOfWork.Users.AnyAsync(
                        u => u.Email == request.Email && u.Id != request.UserId,
                        cancellationToken);

                    if (emailExists)
                    {
                        _logger?.LogWarning("Email {Email} is already taken by another user ", request.Email);
                        return Result.Failure("Email is already in use by another user");
                    }

                    user.UpdateEmail(request.Email);
                    _logger?.LogInformation("User {UserId} updated email to {Email}", request.UserId, request.Email);
                }

                // Check if display name is already taken by another user
                if (!string.IsNullOrWhiteSpace(request.DisplayName) && request.DisplayName != user.DisplayName)
                {
                    var displayNameExists = await _unitOfWork.Users.AnyAsync(
                        u => u.DisplayName == request.DisplayName && u.Id != request.UserId,
                        cancellationToken);

                    if (displayNameExists)
                    {
                        _logger?.LogWarning("Display name {DisplayName} is already taken by another user", request.DisplayName);
                        return Result.Failure("Display name is already in use by another user");
                    }

                    user.ChangeDisplayName(request.DisplayName);
                    _logger?.LogInformation("User {UserId} updated display name to {DisplayName}", request.UserId, request.DisplayName);
                }


                // Always update notes, even if empty/null (to allow clearing notes)
                user.UpdateNotes(request.Notes);

                // Update avatar URL (allow null to clear avatar)
                user.UpdateAvatarUrl(request.AvatarUrl);

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("User {UserId} updated successfully", request.UserId);
                return Result.Success();
            }
            catch (NotFoundException ex)
            {
                _logger?.LogError(ex, "User {UserId} not found", request.UserId);
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user {UserId}", request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}