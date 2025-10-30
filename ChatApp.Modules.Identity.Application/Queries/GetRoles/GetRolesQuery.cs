using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Queries.GetRoles
{
    public record GetRolesQuery():IRequest<Result<List<RoleDto>>>;
}