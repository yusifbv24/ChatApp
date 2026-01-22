using ChatApp.Modules.Identity.Domain.Constants;
using ChatApp.Shared.Kernel.Common;
using MediatR;

namespace ChatApp.Modules.Identity.Application.Queries.GetPermissions
{
    public record GetPermissionsQuery(
        string? Module
    ) : IRequest<Result<List<string>>>;

    public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, Result<List<string>>>
    {
        public Task<Result<List<string>>> Handle(
            GetPermissionsQuery request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var allPermissions = Permissions.GetAll();

                var permissions = string.IsNullOrWhiteSpace(request.Module)
                    ? allPermissions.ToList()
                    : allPermissions.Where(p => p.StartsWith($"{request.Module}.", StringComparison.OrdinalIgnoreCase)).ToList();

                return Task.FromResult(Result.Success(permissions));
            }
            catch (Exception)
            {
                return Task.FromResult(Result.Failure<List<string>>("An error occurred while retrieving permissions"));
            }
        }
    }
}