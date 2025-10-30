using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Commands.CreateUser
{
    public record CreateUserCommand(
        string Username,
        string Email,
        string Password,
        bool IsAdmin
    ):IRequest<Result<Guid>>;
}