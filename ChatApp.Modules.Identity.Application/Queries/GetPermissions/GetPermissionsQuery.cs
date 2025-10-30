using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Queries.GetPermissions
{
    public record GetPermissionsQuery(
        string? Module
    ): IRequest<Result<List<PermissionDto>>>;
}