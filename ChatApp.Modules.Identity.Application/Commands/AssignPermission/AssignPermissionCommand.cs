using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Commands.AssignPermission
{
    public record AssignPermissionCommand(
        Guid RoleId,
        Guid PermissionId
    ):IRequest<Result>;
}