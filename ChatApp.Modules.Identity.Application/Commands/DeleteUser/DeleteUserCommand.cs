using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Commands.DeleteUser
{
    public record DeleteUserCommand(
        Guid UserId
    ):IRequest<Result>;
}