using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Modules.Identity.Domain.Repositories;
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
        private readonly IUnitOfWork _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IEventBus _eventBus;
        private readonly ILogger<RemoveRoleCommandHandler> _logger;

        public RemoveRoleCommandHandler(
            IUnitOfWork userRepository,
            IRoleRepository roleRepository,
            IEventBus eventBus,
            ILogger<RemoveRoleCommandHandler> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _eventBus = eventBus;
            _logger = logger;
        }


        public async Task<Result> Handle(
            RemoveRoleCommand request,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Removing role {RoleId} from user {UserId}", request.RoleId, request.UserId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                throw new NotFoundException($"User with ID {request.UserId} not found");

            var role=await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
                throw new NotFoundException($"Role with ID {request.RoleId} not found");

            var userRole = await _userRepository.GetUserWithRoleAsync(request.UserId, request.RoleId,cancellationToken);
            if(userRole == null)
            {
                return Result.Failure("User has not role yet");
            }

            user.RemoveRole(userRole.RoleId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            await _eventBus.PublishAsync(new RoleRemovedEvent(request.UserId, request.RoleId),cancellationToken);
            _logger?.LogInformation("Role {RoleId} removed from user {UserId} successfully", request.RoleId, request.UserId);
            return Result.Success();
        }
    }
}