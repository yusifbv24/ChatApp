using ChatApp.Modules.Channels.Application.Commands.Channels;
using ChatApp.Modules.Channels.Application.Commands.ChannelMembers;
using ChatApp.Modules.Channels.Application.DTOs.Requests;
using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Queries.GetChannel;
using ChatApp.Modules.Channels.Application.Queries.GetPublicChannels;
using ChatApp.Modules.Channels.Application.Queries.GetUserChannels;
using ChatApp.Modules.Channels.Application.Queries.SearchChannels;
using ChatApp.Shared.Infrastructure.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Modules.Channels.Api.Controllers
{
    /// <summary>
    /// Controller for managing channels (create, update, delete, query)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChannelsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ChannelsController(
            IMediator mediator)
        {
            _mediator = mediator;
        }



        /// <summary>
        /// Creates a new channel
        /// </summary>
        [HttpPost]
        [RequirePermission("Groups.Create")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateChannel(
            [FromBody] CreateChannelCommand command,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var commandWithUser = command with { CreatedBy = userId };

            var result = await _mediator.Send(commandWithUser, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetChannel),
                new { channelId = result.Value },
                new { channelId = result.Value, message = "Channel created successfully" });
        }



        /// <summary>
        /// Gets a specific channel by ID with full details
        /// </summary>
        [HttpGet("{channelId:guid}")]
        [RequirePermission("Groups.Read")]
        [ProducesResponseType(typeof(ChannelDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetChannel(
            [FromRoute] Guid channelId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new GetChannelQuery(channelId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            if (result.Value == null)
                return NotFound(new { error = $"Channel with ID {channelId} not found" });

            return Ok(result.Value);
        }



        /// <summary>
        /// Gets all channels the current user is a member of
        /// </summary>
        [HttpGet("my-channels")]
        [RequirePermission("Groups.Read")]
        [ProducesResponseType(typeof(List<ChannelDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyChannels(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new GetUserChannelsQuery(userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }



        /// <summary>
        /// Gets all public channels
        /// </summary>
        [HttpGet("public")]
        [RequirePermission("Groups.Read")]
        [ProducesResponseType(typeof(List<ChannelDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPublicChannels(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPublicChannelsQuery(), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }



        /// <summary>
        /// Searches channels by name or description
        /// </summary>
        [HttpGet("search")]
        [RequirePermission("Groups.Read")]
        [ProducesResponseType(typeof(List<ChannelDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SearchChannels(
            [FromQuery] string query,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { error = "Search query cannot be empty" });

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new SearchChannelsQuery(query, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }



        /// <summary>
        /// Updates channel information (name, description, type)
        /// </summary>
        [HttpPut("{channelId:guid}")]
        [RequirePermission("Groups.Manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateChannel(
            [FromRoute] Guid channelId,
            [FromBody] UpdateChannelRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var command = new UpdateChannelCommand(
                channelId,
                request.Name,
                request.Description,
                request.Type,
                userId);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Channel updated successfully" });
        }



        /// <summary>
        /// Deletes (archives) a channel - only owner can delete
        /// </summary>
        [HttpDelete("{channelId:guid}")]
        [RequirePermission("Groups.Manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteChannel(
            [FromRoute] Guid channelId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new DeleteChannelCommand(channelId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Channel deleted successfully" });
        }



        /// <summary>
        /// Toggle pin status for a channel
        /// </summary>
        [HttpPost("{channelId:guid}/toggle-pin")]
        [RequirePermission("Groups.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> TogglePinChannel(
            [FromRoute] Guid channelId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new TogglePinChannelCommand(channelId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { isPinned = result.Value });
        }



        /// <summary>
        /// Toggle mute status for a channel
        /// </summary>
        [HttpPost("{channelId:guid}/toggle-mute")]
        [RequirePermission("Groups.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ToggleMuteChannel(
            [FromRoute] Guid channelId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new ToggleMuteChannelCommand(channelId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { isMuted = result.Value });
        }



        /// <summary>
        /// Toggle mark as read later for a channel
        /// </summary>
        [HttpPost("{channelId:guid}/toggle-read-later")]
        [RequirePermission("Groups.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ToggleMarkChannelAsReadLater(
            [FromRoute] Guid channelId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new ToggleMarkChannelAsReadLaterCommand(channelId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { isMarkedReadLater = result.Value });
        }


        /// <summary>
        /// Unmark channel as read later (clears both conversation-level and message-level marks)
        /// </summary>
        [HttpDelete("{channelId:guid}/read-later")]
        [RequirePermission("Groups.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UnmarkChannelAsReadLater(
            [FromRoute] Guid channelId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new UnmarkChannelReadLaterCommand(channelId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok();
        }


        /// <summary>
        /// Mark all unread channel messages as read and clear all read later flags
        /// </summary>
        [HttpPost("{channelId:guid}/messages/mark-all-read")]
        [RequirePermission("Groups.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAllChannelMessagesAsRead(
            [FromRoute] Guid channelId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new MarkAllChannelMessagesAsReadCommand(channelId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { markedCount = result.Value, message = "All messages marked as read" });
        }


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