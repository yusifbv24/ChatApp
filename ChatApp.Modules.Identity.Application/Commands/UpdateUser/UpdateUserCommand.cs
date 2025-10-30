using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Commands.UpdateUser
{
    public record UpdateUserCommand(
        Guid UserId,
        string? Email,
        bool? IsActive
    ):IRequest<Result>;
}