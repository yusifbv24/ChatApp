using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Commands.Login
{
    public record LoginCommand(
        string Username,
        string Password
    ):IRequest<Result<LoginResponse>>;
}