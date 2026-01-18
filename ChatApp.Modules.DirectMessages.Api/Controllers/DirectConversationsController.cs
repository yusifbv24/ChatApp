using ChatApp.Modules.DirectMessages.Application.Commands.DirectConversations;
using ChatApp.Modules.DirectMessages.Application.DTOs.Request;
using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Queries;
using ChatApp.Shared.Infrastructure.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Modules.DirectMessages.Api.Controllers
{
    [ApiController]
    [Route("api/conversations")]
    [Authorize]
    public class DirectConversationsController:ControllerBase
    {
        private readonly IMediator _mediator;
        public DirectConversationsController(
            IMediator medator)
        {
            _mediator = medator;
        }



        /// <summary>
        /// Gets all conversations for the current user
        /// </summary>
        [HttpGet]
        [RequirePermission("Messages.Read")]
        [ProducesResponseType(typeof(List<DirectConversationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new GetConversationsQuery(userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }



        /// <summary>
        /// Starts a new conversation with another user
        /// </summary>
        [HttpPost]
        [RequirePermission("Messages.Send")]
        [ProducesResponseType(typeof(Guid),StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> StartConversation(
            [FromBody] StartConversationRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new StartConversationCommand(userId, request.OtherUserId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetConversations),
                new { conversationId = result.Value },
                new { conversationId = result.Value, message = "Conversation started succesfully" });
        }



        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim,out var userId))
            {
                return Guid.Empty;
            }

            return userId;
        }
    }
}