using ChatApp.Modules.Channels.Application.Commands.Channels;
using ChatApp.Modules.Channels.Application.DTOs.Requests;
using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Queries.GetChannel;
using ChatApp.Modules.Channels.Application.Queries.GetPublicChannels;
using ChatApp.Modules.Channels.Application.Queries.GetUserChannels;
using ChatApp.Modules.Channels.Application.Queries.SearchChannels;
using ChatApp.Modules.Channels.Domain.Enums;
using ChatApp.Shared.Infrastructure.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ChannelsController> _logger;

        public ChannelsController(
            IMediator mediator,
            ILogger<ChannelsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
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