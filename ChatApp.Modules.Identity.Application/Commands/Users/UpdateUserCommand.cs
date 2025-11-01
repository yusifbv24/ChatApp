﻿using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record UpdateUserCommand(
        Guid UserId,
        string? Email,
        string? DisplayName,
        string? AvatarUrl,
        string? Notes,
        bool? IsActive,
        bool? IsAdmin = false
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


            When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl), () =>
            {
                RuleFor(x => x.AvatarUrl)
                    .MaximumLength(500).WithMessage("Avatar url must not exceed 500 characters");
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

                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);

                if (user == null)
                    throw new NotFoundException($"User with ID {request.UserId} not found");

                if(!string.IsNullOrWhiteSpace(request.Email))
                    user.UpdateEmail(request.Email);

                if(!string.IsNullOrWhiteSpace(request.DisplayName))
                {
                    var existingDisplayName=await _unitOfWork.Users.ExistsAsync(
                        c=>c.DisplayName==request.DisplayName,
                        cancellationToken);

                    if (existingDisplayName)
                    {
                        return Result.Failure($"Display name already exists");
                    }

                    user.ChangeDisplayName(request.DisplayName);
                }


                if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                {
                    user.ChangeAvatar(request.AvatarUrl);
                }


                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    user.UpdateNotes(request.Notes);
                }


                if (request.IsActive.HasValue)
                {
                    if (request.IsActive.Value)
                        user.Activate();
                    else
                        user.Deactivate();
                }


                if (request.IsAdmin.HasValue)
                {
                    if (request.IsAdmin.Value)
                        user.MakeAdmin();
                    else
                        user.RevokeAdmin();
                }


                await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("User {UserId} updated successfully", request.UserId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user {UserId}", request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}