using ChatApp.Modules.Channels.Application.Commands.ChannelMessages;
using ChatApp.Modules.Channels.Application.Commands.ChannelReactions;
using ChatApp.Modules.Channels.Application.Commands.MessageConditions;
using ChatApp.Modules.Channels.Application.DTOs.Requests;
using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Queries.GetChannelMessages;
using ChatApp.Modules.Channels.Application.Queries.GetPinnedMessages;
using ChatApp.Modules.Channels.Application.Queries.GetUnreadCount;
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
    /// Controller for managing channel messages
    /// </summary>
    [ApiController]
    [Route("api/channels/{channelId:guid}/messages")]
    [Authorize]
    public class ChannelMessagesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ChannelMessagesController> _logger;

        public ChannelMessagesController(
            IMediator mediator,
            ILogger<ChannelMessagesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        


        /// <summary>
        /// Gets messages in a channel with pagination (newest first by default)
        /// </summary>
        [HttpGet]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(typeof(List<ChannelMessageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMessages(
            [FromRoute] Guid channelId,
            [FromQuery] int pageSize = 50,
            [FromQuery] DateTime? before = null,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            // Validate pagination
            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "Page size must be between 1 and 100" });

            var result = await _mediator.Send(
                new GetChannelMessagesQuery(channelId, userId, pageSize, before),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }




        /// <summary>
        /// Gets pinned messages in a channel
        /// </summary>
        [HttpGet("pinned")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(typeof(List<ChannelMessageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPinnedMessages(
            [FromRoute] Guid channelId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new GetPinnedMessagesQuery(channelId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }



        /// <summary>
        /// Gets unread message count for the current user in this channel
        /// </summary>
        [HttpGet("unread-count")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUnreadCount(
            [FromRoute] Guid channelId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new GetUnreadCountQuery(channelId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { unreadCount = result.Value });
        }



        /// <summary>
        /// Sends a message to the channel
        /// </summary>
        [HttpPost]
        [RequirePermission("Messages.Send")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SendMessage(
            [FromRoute] Guid channelId,
            [FromBody] SendMessageRequestToChannel request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new SendChannelMessageCommand(channelId, userId, request.Content, request.FileId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetMessages),
                new { channelId },
                new { messageId = result.Value, message = "Message sent successfully" });
        }



        /// <summary>
        /// Edits a message (only sender can edit)
        /// </summary>
        [HttpPut("{messageId:guid}")]
        [RequirePermission("Messages.Edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> EditMessage(
            [FromRoute] Guid channelId,
            [FromRoute] Guid messageId,
            [FromBody] EditMessageRequestToChannel request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new EditChannelMessageCommand(messageId, request.NewContent, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Message edited successfully" });
        }



        /// <summary>
        /// Deletes a message (sender, admin, or owner can delete)
        /// </summary>
        [HttpDelete("{messageId:guid}")]
        [RequirePermission("Messages.Delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteMessage(
            [FromRoute] Guid channelId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new DeleteChannelMessageCommand(messageId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Message deleted successfully" });
        }




        /// <summary>
        /// Pins a message (admin/owner only)
        /// </summary>
        [HttpPost("{messageId:guid}/pin")]
        [RequirePermission("Groups.Manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PinMessage(
            [FromRoute] Guid channelId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new PinMessageCommand(messageId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Message pinned successfully" });
        }



        /// <summary>
        /// Unpins a message (admin/owner only)
        /// </summary>
        [HttpDelete("{messageId:guid}/pin")]
        [RequirePermission("Groups.Manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UnpinMessage(
            [FromRoute] Guid channelId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new UnpinMessageCommand(messageId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Message unpinned successfully" });
        }



        /// <summary>
        /// Adds a reaction to a message
        /// </summary>
        [HttpPost("{messageId:guid}/reactions")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddReaction(
            [FromRoute] Guid channelId,
            [FromRoute] Guid messageId,
            [FromBody] AddReactionRequestToChannel request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new AddReactionCommand(messageId, userId, request.Reaction),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Reaction added successfully" });
        }



        /// <summary>
        /// Removes a reaction from a message
        /// </summary>
        [HttpDelete("{messageId:guid}/reactions")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemoveReaction(
            [FromRoute] Guid channelId,
            [FromRoute] Guid messageId,
            [FromBody] RemoveReactionRequestToChannel request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new RemoveReactionCommand(messageId, userId, request.Reaction),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Reaction removed successfully" });
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