using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record DeleteUserCommand(
        Guid UserId
    ) : IRequest<Result>;


    public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
    {
        public DeleteUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID cannot be empty to delete user");
        }
    }



    public class DeleteUserCommandHandler:IRequestHandler<DeleteUserCommand,Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;
        private readonly ILogger<DeleteUserCommand> _logger;
        public DeleteUserCommandHandler(
            IUnitOfWork unitOfWork,
            IEventBus eventBus,
            ILogger<DeleteUserCommand> logger)
        {
            _unitOfWork=unitOfWork;
            _eventBus=eventBus;
            _logger=logger;
        }


        public async Task<Result> Handle(
            DeleteUserCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Deleting user {UserId}", request.UserId);

                var user = await _unitOfWork.Users
                    .FirstOrDefaultAsync(r=>r.Id==request.UserId, cancellationToken);

                if (user == null)
                    throw new NotFoundException($"User with ID {request.UserId} not found");

                _unitOfWork.Users.Remove(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _eventBus.PublishAsync(new UserDeletedEvent(user.Id, user.Username), cancellationToken);

                _logger?.LogInformation("User {UserId} deleted successfully", request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting user {UserId}", request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}