using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record GrantUserPermissionCommand(
        Guid UserId,
        Guid PermissionId,
        Guid? GrantedBy
    ) : IRequest<Result>;

    public class GrantUserPermissionCommandHandler : IRequestHandler<GrantUserPermissionCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GrantUserPermissionCommand> _logger;

        public GrantUserPermissionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<GrantUserPermissionCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            GrantUserPermissionCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Granting permission {PermissionId} to user {UserId}", request.PermissionId, request.UserId);

                var user = await _unitOfWork.Users
                    .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

                if (user == null)
                    throw new NotFoundException($"User with ID {request.UserId} not found");

                var permission = await _unitOfWork.Permissions
                    .FirstOrDefaultAsync(p => p.Id == request.PermissionId, cancellationToken);

                if (permission == null)
                    throw new NotFoundException($"Permission with ID {request.PermissionId} not found");

                var userPermission = new UserPermission(
                    request.UserId,
                    request.PermissionId,
                    true, // isGranted = true
                    request.GrantedBy);

                user.GrantPermission(userPermission);

                await _unitOfWork.UserPermissions.AddAsync(userPermission, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("Permission {PermissionId} granted to user {UserId} successfully", request.PermissionId, request.UserId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error granting permission {PermissionId} to user {UserId}", request.PermissionId, request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}
