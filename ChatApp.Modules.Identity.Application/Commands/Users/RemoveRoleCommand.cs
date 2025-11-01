using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record RemoveRoleCommand(
        Guid UserId,
        Guid RoleId
    ):IRequest<Result>;


    public class RemoveRoleCommandValidator : AbstractValidator<RemoveRoleCommand>
    {
        public RemoveRoleCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role ID is required");
        }
    }


    public class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;
        private readonly ILogger<RemoveRoleCommandHandler> _logger;

        public RemoveRoleCommandHandler(
            IUnitOfWork unitOfWork,
            IEventBus eventBus,
            ILogger<RemoveRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _eventBus = eventBus;
            _logger = logger;
        }


        public async Task<Result> Handle(
            RemoveRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Removing role {RoleId} from user {UserId}", request.RoleId, request.UserId);

            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                throw new NotFoundException($"User with ID {request.UserId} not found");

            var role=await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
                throw new NotFoundException($"Role with ID {request.RoleId} not found");

            var userRole = await _unitOfWork.UserRoles.GetFirstOrDefaultAsync(
                u=>u.UserId==request.UserId &&
                u.RoleId==request.RoleId,
                cancellationToken);

            if(userRole == null)
            {
                return Result.Failure("User has not role yet");
            }

            user.RemoveRole(userRole.RoleId);
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _eventBus.PublishAsync(new RoleRemovedEvent(request.UserId, request.RoleId),cancellationToken);
            _logger?.LogInformation("Role {RoleId} removed from user {UserId} successfully", request.RoleId, request.UserId);
            return Result.Success();
        }
    }
}