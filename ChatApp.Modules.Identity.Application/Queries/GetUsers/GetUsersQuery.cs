using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Queries.GetUsers
{
    public record GetUsersQuery(
        int PageNumber,
        int PageSize
    ):IRequest<Result<List<UserDto>>>;
}