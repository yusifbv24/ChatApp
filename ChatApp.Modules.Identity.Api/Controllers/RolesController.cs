using ChatApp.Modules.Identity.Application.Commands.Roles;
using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Modules.Identity.Application.Queries.GetRoles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Api.Controllers
{
    /// <summary>
    /// Controller for managing roles and their associated permissions
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RolesController> _logger;

        public RolesController(
            IMediator mediator,
            ILogger<RolesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new role in the system
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateRole(
            [FromBody] CreateRoleCommand command,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Creating role: {RoleName}", command.Name);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetRoles),
                new { roleId = result.Value },
                new { roleId = result.Value, message = "Role created successfully" });
        }

        /// <summary>
        /// Retrieves all roles in the system
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Retrieving all roles");

            var result = await _mediator.Send(new GetRolesQuery(), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }

        /// <summary>
        /// Updates an existing role's information
        /// </summary>
        [HttpPut("{roleId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateRole(
            [FromRoute] Guid roleId,
            [FromBody] UpdateRoleCommand command,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Updating role: {RoleId}", roleId);

            // Ensure the route parameter matches the command
            var commandWithRoleId = command with { RoleId = roleId };

            var result = await _mediator.Send(commandWithRoleId, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Role updated successfully" });
        }

        /// <summary>
        /// Deletes a role from the system
        /// </summary>
        [HttpDelete("{roleId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteRole(
            [FromRoute] Guid roleId,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Deleting role: {RoleId}", roleId);

            var result = await _mediator.Send(new DeleteRoleCommand(roleId), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Role deleted successfully" });
        }
    }
}