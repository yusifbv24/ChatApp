using ChatApp.Modules.DirectMessages.Application.Commands.DirectMessageReactions;
using ChatApp.Modules.DirectMessages.Application.Commands.DirectMessages;
using ChatApp.Modules.DirectMessages.Application.Commands.MessageConditions;
using ChatApp.Modules.DirectMessages.Application.DTOs.Request;
using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Queries;
using ChatApp.Modules.DirectMessages.Application.Queries.GetPinnedMessages;
using ChatApp.Shared.Infrastructure.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChatApp.Modules.DirectMessages.Api.Controllers
{
    /// <summary>
    /// Controller for managing direct messages within conversations
    /// </summary>
    [ApiController]
    [Route("api/conversations/{conversationId:guid}/messages")]
    [Authorize]
    public class DirectMessagesController:ControllerBase
    {
        private readonly IMediator _mediator;

        public DirectMessagesController(
            IMediator mediator)
        {
            _mediator = mediator;
        }


        /// <summary>
        /// Gets messages in a conversation with pagination (newest first by default)
        /// </summary>
        [HttpGet]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(typeof(List<DirectMessageDto>),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMessages(
            [FromRoute] Guid conversationId,
            [FromQuery] int pageSize=50,
            [FromQuery] DateTime? before=null,
            CancellationToken cancellationToken =default)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            // Validate pagination
            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "Page size must be between 1 and 100" });

            var result = await _mediator.Send(
                new GetConversationMessagesQuery(conversationId, userId, pageSize, before),
                cancellationToken);

            if(result.IsFailure)
                return BadRequest(new {error=result.Error});

            return Ok(result.Value);
        }




        /// <summary>
        /// Gets unread message count for the current user in this conversation
        /// </summary>
        [HttpGet("unread-count")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(typeof(int),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUnreadCount(
            [FromRoute] Guid conversationId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new GetUnreadCountQuery(conversationId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new {unreadCount=result.Value});
        }




        /// <summary>
        /// Gets pinned messages in a conversation
        /// </summary>
        [HttpGet("pinned")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(typeof(List<DirectMessageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPinnedMessages(
            [FromRoute] Guid conversationId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new GetPinnedMessagesQuery(conversationId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }




        /// <summary>
        /// Sends a message in the conversation
        /// </summary>
        [HttpPost]
        [RequirePermission("Messages.Send")]
        [ProducesResponseType(typeof(Guid),StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SendMessage(
            [FromRoute] Guid conversationId,
            [FromBody] SendMessageRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new SendDirectMessageCommand(
                    conversationId,
                    userId,
                    request.Content,
                    request.FileId,
                    request.ReplyToMessageId,
                    request.IsForwarded),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new {error=result.Error});

            return CreatedAtAction(
                nameof(GetMessages),
                new { conversationId },
                new { messageId = result.Value, message = "Message sent succesfully" });
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
            [FromRoute] Guid conversationId,
            [FromRoute] Guid messageId,
            [FromBody] EditMessageRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if(userId==Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new EditDirectMessageCommand(messageId, request.NewContent, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new {message="Message edited succesfully"});
        }



        /// <summary>
        /// Deletes a message (only sender can delete)
        /// </summary>
        [HttpDelete("{messageId:guid}")]
        [RequirePermission("Messages.Delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteMessage(
            [FromRoute] Guid conversationId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if(userId==Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new DeleteDirectMessageCommand(messageId, userId),
                cancellationToken);

            if(result.IsFailure)
                return BadRequest(new {error=result.Error});

            return Ok(new { message = "Message deleted succesfully" });
        }



        /// <summary>
        /// Marks all unread messages in the conversation as read (bulk operation)
        /// </summary>
        [HttpPost("mark-as-read")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAllAsRead(
            [FromRoute] Guid conversationId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new MarkDirectMessagesAsReadCommand(conversationId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Messages marked as read" });
        }


        /// <summary>
        /// Marks a message as read (only receiver can mark as read)
        /// </summary>
        [HttpPost("{messageId:guid}/read")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MarkAsRead(
            [FromRoute] Guid conversationId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if(userId== Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new MarkMessageAsReadCommand(messageId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new {error=result.Error});

            return Ok(new { message = "Message marked as read" });
        }




        /// <summary>
        /// Toggles a reaction on a message (add/remove/replace)
        /// </summary>
        [HttpPut("{messageId:guid}/reactions/toggle")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ToggleReaction(
            [FromRoute] Guid conversationId,
            [FromRoute] Guid messageId,
            [FromBody] DirectMessageToggleReactionRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new ToggleReactionCommand(messageId, userId, request.Reaction),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }



        /// <summary>
        /// Toggles a message as "Read Later" for the current user
        /// If same message clicked -> unmark (toggle off)
        /// If different message clicked -> mark new one (auto-switches)
        /// </summary>
        [HttpPost("{messageId:guid}/mark-later/toggle")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ToggleMessageAsLater(
            [FromRoute] Guid conversationId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new ToggleMessageAsLaterCommand(conversationId, messageId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Read later toggled successfully" });
        }




        /// <summary>
        /// Pins a message
        /// </summary>
        [HttpPost("{messageId:guid}/pin")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PinMessage(
            [FromRoute] Guid conversationId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new PinDirectMessageCommand(messageId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Message pinned successfully" });
        }




        /// <summary>
        /// Unpins a message
        /// </summary>
        [HttpDelete("{messageId:guid}/pin")]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UnpinMessage(
            [FromRoute] Guid conversationId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new UnpinDirectMessageCommand(messageId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Message unpinned successfully" });
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