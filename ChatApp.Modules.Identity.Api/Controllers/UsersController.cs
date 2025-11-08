using ChatApp.Modules.Identity.Application.Commands.Users;
using ChatApp.Modules.Identity.Application.DTOs.Requests;
using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Queries.GetUser;
using ChatApp.Modules.Identity.Application.Queries.GetUsers;
using ChatApp.Shared.Infrastructure.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChatApp.Modules.Identity.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        [RequirePermission("Users.Create")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateUserRequest request,
            CancellationToken cancellationToken)
        {
            // Get the ID of the user making the request
            var creatorId = GetCurrentUserId();
            if (creatorId == Guid.Empty)
                return Unauthorized();

            // Create a new command
            var command = new CreateUserCommand(
                request.Username,
                request.Email,
                request.Password,
                request.DisplayName,
                request.CreatedBy,
                request.IsAdmin,
                request.AvatarUrl,
                request.Notes);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetUserById),
                new { userId = result.Value },
                new { userId=result.Value, message="User created succesfully"});
        }




        /// <summary>
        /// Gets the current authenticated user's profile information
        /// This endpoint does not require any permissions - any authenticated user can view their own profile
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            _logger?.LogInformation("User {UserId} retrieving their own profile", userId);

            var result = await _mediator.Send(new GetCurrentUserQuery(userId), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            if (result.Value == null)
                return NotFound(new { error = "Your profile was not found" });

            return Ok(result.Value);
        }




        /// <summary>
        /// Updates the current authenticated user's profile information
        /// Users can update their email, display name, avatar URL, and notes
        /// This endpoint does not require any permissions - any authenticated user can update their own profile
        /// </summary>
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateCurrentUser(
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var command = new UpdateUserCommand(
                userId,
                request.Email,
                request.DisplayName,
                request.AvatarUrl,
                request.Notes);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Profile updated successfully" });
        }




        /// <summary>
        /// Changes the current authenticated user's password
        /// Requires the current password for security verification
        /// This endpoint does not require any permissions - any authenticated user can change their own password
        /// </summary>
        [HttpPost("me/change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var command = new ChangePasswordCommand(
                userId,
                request.CurrentPassword,
                request.NewPassword,
                request.ConfirmNewPassword);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Password changed successfully" });
        }




        /// <summary>
        /// Changes the current authenticated user's password
        /// Requires the current password for security verification
        /// This endpoint does not require any permissions - any authenticated user can change their own password
        /// </summary>
        [HttpPost("change-password/{id:guid}")]
        [RequirePermission("Users.Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ChangeUserPassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var command = new ChangePasswordCommand(
                request.UserId,
                request.CurrentPassword,
                request.NewPassword,
                request.ConfirmNewPassword);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Password changed successfully" });
        }




        /// <summary>
        /// Retrieves a specific user by their unique identifier
        /// </summary>
        [HttpGet("{userId:guid}")]
        [RequirePermission("Users.Read")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        [RequirePermission("Users.Read")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        [RequirePermission("Users.Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
                request.Notes);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "User updated successfully" });
        }




        [HttpPut("{userId:guid}/activate")]
        [RequirePermission("Users.Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ActivateUser(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            var command = new ActivateUserCommand(
                userId);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "User activated successfully" });
        }




        [HttpPut("{userId:guid}/deactivate")]
        [RequirePermission("Users.Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeactivateUser(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            var command = new DeactivateUserCommand(
                userId);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "User deactivated successfully" });
        }




        /// <summary>
        /// Soft deletes a user by deactivating their account
        /// </summary>
        [HttpDelete("{userId:guid}")]
        [RequirePermission("Users.Delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        [RequirePermission("Users.Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        [RequirePermission("Users.Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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