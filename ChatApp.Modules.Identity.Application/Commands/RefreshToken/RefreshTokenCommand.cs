using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Commands.RefreshToken
{
    public record RefreshTokenCommand(
        string RefreshToken
    ):IRequest<Result<RefreshTokenResponse>>;
}