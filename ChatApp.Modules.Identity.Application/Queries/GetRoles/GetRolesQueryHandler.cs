using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetRoles
{
    public class GetRolesQueryHandler
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<GetRolesQueryHandler> _logger;

        public GetRolesQueryHandler(
            IRoleRepository roleRepository,
            ILogger<GetRolesQueryHandler> logger)
        {
            _roleRepository= roleRepository;
            _logger= logger;
        }

        public async Task<Result<List<RoleDto>>> HandleAsync(GetRolesQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                var roles = await _roleRepository.GetAllAsync(cancellationToken);

                var roleDtos=roles.Select(r=>new RoleDto
                {
                    Id=r.Id,
                    Name=r.Id,
                    Description=r.Description,
                    IsSystemRole=r.IsSystemRole
                }).ToList();

                return Result.Success(roleDtos);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving roles");
                return Result.Failure<List<RoleDto>>("An error occurred while retrieving roles");
            }
        }
    }
}