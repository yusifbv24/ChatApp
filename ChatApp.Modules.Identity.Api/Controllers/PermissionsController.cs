using ChatApp.Modules.Identity.Application.Commands.Permisisons;
using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Modules.Identity.Application.Queries.GetPermissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Modules.Identity.Api.Controllers
{
    /// <summary>
    /// Controller for managing permissions and their assignment to roles
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(
            IMediator mediator,
            ILogger<PermissionsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all permissions in the system, optionally filtered by module
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPermissions(
            [FromQuery] string? module = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving permissions for module: {Module}", module ?? "all");

            var result = await _mediator.Send(new GetPermissionsQuery(module), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }

        /// <summary>
        /// Assigns a permission to a role
        /// </summary>
        [HttpPost("roles/{roleId:guid}/permissions/{permissionId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AssignPermissionToRole(
            [FromRoute] Guid roleId,
            [FromRoute] Guid permissionId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);

            var result = await _mediator.Send(
                new AssignPermissionCommand(roleId, permissionId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Permission assigned to role successfully" });
        }

        /// <summary>
        /// Removes a permission from a role
        /// </summary>
        [HttpDelete("roles/{roleId:guid}/permissions/{permissionId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RemovePermissionFromRole(
            [FromRoute] Guid roleId,
            [FromRoute] Guid permissionId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Removing permission {PermissionId} from role {RoleId}", permissionId, roleId);

            var result = await _mediator.Send(
                new RemovePermissionCommand(roleId, permissionId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Permission removed from role successfully" });
        }
    }
}