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


    public class RemoveRoleCommandHandler(
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        ILogger<RemoveRoleCommandHandler> logger) : IRequestHandler<RemoveRoleCommand, Result>
    {
        public async Task<Result> Handle(
            RemoveRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            logger?.LogInformation("Removing role {RoleId} from user {UserId}", request.RoleId, request.UserId);

            var user = await unitOfWork.Users
                .FirstOrDefaultAsync(r=>r.Id==request.UserId, cancellationToken) 
                    ?? throw new NotFoundException($"User with ID {request.UserId} not found");

            var role = await unitOfWork.Roles
                .FirstOrDefaultAsync(r=>r.Id==request.RoleId, cancellationToken) 
                    ?? throw new NotFoundException($"Role with ID {request.RoleId} not found");

            var userRole = await unitOfWork.UserRoles.FirstOrDefaultAsync(
                u=>u.UserId==request.UserId &&
                u.RoleId==request.RoleId,
                cancellationToken);

            if(userRole == null)
            {
                return Result.Failure("User has not role yet");
            }

            user.RemoveRole(userRole.RoleId);
            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await eventBus.PublishAsync(new RoleRemovedEvent(request.UserId, request.RoleId),cancellationToken);
            logger?.LogInformation("Role {RoleId} removed from user {UserId} successfully", request.RoleId, request.UserId);
            return Result.Success();
        }
    }
}