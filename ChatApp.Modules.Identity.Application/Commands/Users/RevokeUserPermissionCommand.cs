using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Users
{
    public record RevokeUserPermissionCommand(
        Guid UserId,
        Guid PermissionId
    ) : IRequest<Result>;

    public class RevokeUserPermissionCommandHandler : IRequestHandler<RevokeUserPermissionCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RevokeUserPermissionCommand> _logger;

        public RevokeUserPermissionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<RevokeUserPermissionCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            RevokeUserPermissionCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Revoking permission {PermissionId} from user {UserId}", request.PermissionId, request.UserId);

                var user = await _unitOfWork.Users
                    .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

                if (user == null)
                    throw new NotFoundException($"User with ID {request.UserId} not found");

                var userPermission = await _unitOfWork.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == request.UserId && up.PermissionId == request.PermissionId, cancellationToken);

                if (userPermission != null)
                {
                    user.RevokePermission(request.PermissionId);
                    _unitOfWork.UserPermissions.Remove(userPermission);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                _logger?.LogInformation("Permission {PermissionId} revoked from user {UserId} successfully", request.PermissionId, request.UserId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error revoking permission {PermissionId} from user {UserId}", request.PermissionId, request.UserId);
                return Result.Failure(ex.Message);
            }
        }
    }
}
