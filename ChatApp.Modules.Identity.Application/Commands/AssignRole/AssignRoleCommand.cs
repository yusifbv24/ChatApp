using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Commands.AssignRole
{
    public record AssignRoleCommand(
        Guid UserId,
        Guid RoleId,
        Guid? AssignedBy
    ):IRequest<Result>;
}