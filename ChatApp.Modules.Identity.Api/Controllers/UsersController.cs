using ChatApp.Modules.Identity.Application.Commands.Users;
using ChatApp.Modules.Identity.Application.DTOs;
using ChatApp.Modules.Identity.Application.Queries.GetUser;
using ChatApp.Modules.Identity.Application.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChatApp.Modules.Identity.Api.Controllers
{
    /// <summary>
    /// Controller for managing user operations including creation, updates, role assignment, and retrieval
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication by default
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IMediator mediator,
            ILogger<UsersController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }


        /// <summary>
        /// Creates a new user in the system
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateUserCommand command,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Creating user: {Username}", command.Username);

            // Get the ID of the user making the request
            var creatorId = GetCurrentUserId();
            if (creatorId == Guid.Empty)
                return Unauthorized();

            // Create a new command with the creator's ID
            var commandWithCreator = command with { CreatedBy = creatorId };

            var result = await _mediator.Send(commandWithCreator, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetUserById),
                new { userId = result.Value },
                new { userId=result.Value, message="User created succesfully"});
        }



        /// <summary>
        /// Retrieves a specific user by their unique identifier
        /// </summary>
        [HttpGet("{userId:guid}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUserById(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUserQuery(userId), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            if (result.Value == null)
                return NotFound(new { error = $"User with ID {userId} not found" });

            return Ok(result.Value);
        }



        /// <summary>
        /// Retrieves a paginated list of all users
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            // Validate pagination parameters
            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "Invalid pagination parameters. PageNumber must be >= 1 and PageSize must be between 1 and 100" });

            var result = await _mediator.Send(
                new GetUsersQuery(pageNumber, pageSize),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }



        /// <summary>
        /// Updates an existing user's information
        /// </summary>
        [HttpPut("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateUser(
            [FromRoute] Guid userId,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateUserCommand(
                userId,
                request.Email,
                request.DisplayName,
                request.AvatarUrl,
                request.Notes,
                request.IsActive);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "User updated successfully" });
        }



        /// <summary>
        /// Soft deletes a user by deactivating their account
        /// </summary>
        [HttpDelete("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteUser(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Deleting user: {UserId}", userId);

            var result = await _mediator.Send(new DeleteUserCommand(userId), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "User deleted successfully" });
        }



        /// <summary>
        /// Assigns a role to a user
        /// </summary>
        [HttpPost("{userId:guid}/roles/{roleId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AssignRole(
            [FromRoute] Guid userId,
            [FromRoute] Guid roleId,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Assigning role {RoleId} to user {UserId}", roleId, userId);

            var assignedBy = GetCurrentUserId();
            if (assignedBy == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new AssignRoleCommand(userId, roleId, assignedBy),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Role assigned successfully" });
        }

        /// <summary>
        /// Removes a role from a user
        /// </summary>
        [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RemoveRole(
            [FromRoute] Guid userId,
            [FromRoute] Guid roleId,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Removing role {RoleId} from user {UserId}", roleId, userId);

            var result = await _mediator.Send(
                new RemoveRoleCommand(userId, roleId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Role removed successfully" });
        }

        /// <summary>
        /// Helper method to extract the current user's ID from the JWT token claims
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Guid.Empty;
            }

            return userId;
        }
    }
}