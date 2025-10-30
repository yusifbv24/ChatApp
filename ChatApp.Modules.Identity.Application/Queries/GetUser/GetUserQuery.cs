using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Queries.GetUser
{
    public record GetUserQuery(
        Guid UserId
    ):IRequest<Result<UserDto?>>;
}