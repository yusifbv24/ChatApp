using ChatApp.Modules.Search.Application.DTOs.Requests;
using ChatApp.Modules.Search.Application.Queries.SearchMessages;
using ChatApp.Modules.Search.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChatApp.Modules.Search.Api.Controllers
{
    [ApiController]
    [Route("api/search")]
    [Authorize]
    public class SearchController:ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            IMediator mediator,
            ILogger<SearchController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(SearchResultsDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Search(
            [FromQuery] string q,
            [FromQuery] SearchScope scope=SearchScope.All,
            [FromQuery] Guid? channelId=null,
            [FromQuery] Guid? conversationId=null,
            [FromQuery] int page=1,
            [FromQuery] int pageSize=20,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { error = "Search term is required" });

            var result = await _mediator.Send(
                new SearchMessagesQuery(
                    userId,
                    q.Trim(),
                    scope,
                    channelId,
                    conversationId,
                    page,
                    pageSize),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }




        [HttpGet("channels/{channelId:guid}")]
        [ProducesResponseType(typeof(SearchResultsDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SearchInChannel(
            [FromRoute] Guid conversationId,
            [FromQuery] string q,
            [FromQuery] int page=1,
            [FromQuery] int pageSize=20,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { error = "Search term is required" });

            var result = await _mediator.Send(
                new SearchMessagesQuery(
                    userId,
                    q.Trim(),
                    SearchScope.SpecificConversation,
                    null,
                    conversationId,
                    page,
                    pageSize),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }



        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim,out var userId))
            {
                return Guid.Empty;
            }

            return userId;
        }
    }
}