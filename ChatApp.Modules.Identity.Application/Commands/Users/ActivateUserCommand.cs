using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record ActivateUserCommand(
        Guid UserId
    ):IRequest<Result>;


    public class ActivateUserCommandValidator : AbstractValidator<ActivateUserCommand>
    {
        public ActivateUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }


    public class ActivateUserCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ActivateUserCommandHandler> logger) : IRequestHandler<ActivateUserCommand, Result>
    {
        public async Task<Result> Handle(
            ActivateUserCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var existingUser = await unitOfWork.Users
                    .FirstOrDefaultAsync(r=>r.Id== request.UserId,cancellationToken);

                if(existingUser == null)
                {
                    return Result.Failure($"User with this ID {request.UserId} not found");
                }

                if (existingUser.IsActive)
                {
                    return Result.Success("User already is active");
                }
                existingUser.Activate();

                unitOfWork.Users.Update(existingUser);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error occurred while activating user . Error is : {ex.Message}");
                return Result.Failure(ex.Message);
            }
        }
    }
}