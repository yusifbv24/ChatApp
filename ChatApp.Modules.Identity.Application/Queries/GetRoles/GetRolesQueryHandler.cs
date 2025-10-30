using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetRoles
{
    public class GetRolesQueryHandler:IRequestHandler<GetRolesQuery,Result<List<RoleDto>>>
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

        public async Task<Result<List<RoleDto>>> Handle(
            GetRolesQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var roles = await _roleRepository.GetAllAsync(cancellationToken);

                var roleDtos=roles.Select(r=>new RoleDto
                (
                    r.Id,
                    r.Name,
                    r.Description,
                    r.IsSystemRole
                )).ToList();

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