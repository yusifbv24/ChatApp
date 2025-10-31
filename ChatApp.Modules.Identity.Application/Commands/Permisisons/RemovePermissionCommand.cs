using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Permisisons
{
    public record RemovePermissionCommand(
        Guid RoleId,
        Guid PermissionId
    ):IRequest<Result>;



    public class RemovePermissionCommandValidator : AbstractValidator<RemovePermissionCommand>
    {
        public RemovePermissionCommandValidator()
        {
            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role ID is required");

            RuleFor(x => x.PermissionId)
                .NotEmpty().WithMessage("Permission ID is required");
        }
    }



    public class RemovePermissionCommandHandler:IRequestHandler<RemovePermissionCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemovePermissionCommandHandler> _logger;

        public RemovePermissionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<RemovePermissionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger= logger;
        }


        public async Task<Result> Handle(
            RemovePermissionCommand request,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Removing permission {PermissionId} from role {RoleId}", request.PermissionId, request.RoleId);

            var rolePermission = await _unitOfWork.RolePermissions.GetFirstOrDefaultAsync(
                r=>r.RoleId == request.RoleId
                && r.PermissionId == request.PermissionId,
                cancellationToken);

            if(rolePermission==null)
                throw new NotFoundException($"Permission {request.PermissionId} with this Role {request.RoleId} not found");

            await _unitOfWork.RolePermissions.DeleteAsync(rolePermission,cancellationToken);
            _logger?.LogInformation("Permission {PermissionId} removed from role {RoleId} successfully", request.PermissionId, request.RoleId);
            return Result.Success();
        }
    }
}