using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Modules.Identity.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IMediator mediator,
            ILogger<AuthController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
    }
}
