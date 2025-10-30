using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Commands.CreateRole
{
    public record CreateRoleCommand(
        string Name,
        string Description
    ):IRequest<Result<Guid>>;
}