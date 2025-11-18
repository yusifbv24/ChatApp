using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record DeactivateUserCommand(
        Guid UserId
    ):IRequest<Result>;


    public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
    {
        public DeactivateUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }


    public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeactivateUserCommandHandler> _logger;

        public DeactivateUserCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeactivateUserCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            DeactivateUserCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var existingUser = await _unitOfWork.Users
                    .FirstOrDefaultAsync(r=>r.Id==request.UserId,cancellationToken);

                if(existingUser == null)
                {
                    return Result.Failure($"User with this ID {request.UserId} not found");
                }

                if (!existingUser.IsActive)
                {
                    return Result.Success("User already is deactived");
                }
                existingUser.Deactivate();

                _unitOfWork.Users.Update(existingUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error occurred while deactivating user . Error is : {ex.Message}");
                return Result.Failure(ex.Message);
            }
        }
    }
}