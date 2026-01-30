using ChatApp.Modules.Identity.Application.Queries.Organization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Modules.Identity.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/identity/organization")]
    public class OrganizationController(IMediator mediator) : ControllerBase
    {
        /// <summary>
        /// Get organization hierarchy (departments and users in tree structure)
        /// </summary>
        [HttpGet("hierarchy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetOrganizationHierarchy(CancellationToken cancellationToken)
        {
            var query = new GetOrganizationHierarchyQuery();
            var result = await mediator.Send(query, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }
    }
}
