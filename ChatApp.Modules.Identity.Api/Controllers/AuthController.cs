using ChatApp.Modules.Identity.Application.Commands.Login;
using ChatApp.Modules.Identity.Application.Commands.RefreshToken;
using ChatApp.Modules.Identity.Application.DTOs.Requests;
using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Queries.GetUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChatApp.Modules.Identity.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuthController> _logger;
        private readonly bool _isDevelopment;
        private readonly IConfiguration _configuration;

        public AuthController(
            IMediator mediator,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _mediator = mediator;
            _logger = logger;
            _configuration = configuration;
            _isDevelopment = configuration["ASPNETCORE_ENVIRONMENT"] == "Development";
        }


        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Login request for email: {Email}", request.Email);

            var command = new LoginCommand(request.Email, request.Password, request.RememberMe);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            // Set HttpOnly cookies for tokens (XSS-proof)
            SetAuthCookies(result.Value!.AccessToken, result.Value.RefreshToken, result.Value.ExpiresIn, result.Value.RememberMe);

            return Ok(new { success = true, message = "Login successful" });
        }



        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
        {
            // Read refresh token from HttpOnly cookie
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest(new { error = "No refresh token found" });

            var result = await _mediator.Send(new RefreshTokenCommand(refreshToken), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            // Set new HttpOnly cookies with rotated tokens
            // Use rememberMe=true for refresh operations to maintain persistent session
            SetAuthCookies(result.Value!.AccessToken, result.Value.RefreshToken, result.Value.ExpiresIn, rememberMe: true);

            return Ok(new { message = "Token refreshed successfully" });
        }



        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (userId == Guid.Empty)
                return BadRequest(new { error = "Invalid user session" });

            var result = await _mediator.Send(new GetCurrentUserQuery(userId), cancellationToken);

            if (result.IsFailure || result.Value == null)
                return BadRequest(new { error = "User not found" });

            return Ok(result.Value);
        }



        /// <summary>
        /// Returns the access token for SignalR connection (required because WebSocket doesn't send cookies)
        /// This endpoint requires authentication via cookie, so it's secure - the token is only returned
        /// to authenticated users for establishing SignalR connections.
        /// </summary>
        [HttpGet("signalr-token")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetSignalRToken()
        {
            var accessToken = Request.Cookies["accessToken"];

            if (string.IsNullOrEmpty(accessToken))
                return Unauthorized(new { error = "No access token found" });

            return Ok(new { token = accessToken });
        }


        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result = await _mediator.Send(new LogoutCommand(userId));

            if (result.IsFailure)
            {
                return BadRequest(result);
            }

            // Clear authentication cookies
            ClearAuthCookies();

            _logger?.LogInformation("User {UserId} logged out succesfully", userId);
            return Ok(new { message = "Logout successful" });
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

        /// <summary>
        /// Sets HttpOnly authentication cookies (XSS-proof)
        /// </summary>
        private void SetAuthCookies(string accessToken, string refreshToken, int expiresIn, bool rememberMe)
        {
            var isProduction = !_isDevelopment;
            var refreshTokenExpirationDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 30);

            // Set access token cookie with expiration
            var accessTokenOptions = new CookieOptions
            {
                HttpOnly = true,        // Cannot be accessed by JavaScript (XSS protection)
                Secure = isProduction,  // Only sent over HTTPS in production
                SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
                Path = "/"
            };

            Response.Cookies.Append("accessToken", accessToken, accessTokenOptions);

            // Set refresh token cookie
            // If RememberMe is true, cookie persists for configured days
            // If RememberMe is false, cookie is session-only (expires when browser closes)
            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = rememberMe ? DateTimeOffset.UtcNow.AddDays(refreshTokenExpirationDays) : null,
                Path = "/"
            };

            Response.Cookies.Append("refreshToken", refreshToken, refreshTokenOptions);
        }

        /// <summary>
        /// Clears authentication cookies on logout
        /// </summary>
        private void ClearAuthCookies()
        {
            var isProduction = !_isDevelopment;

            Response.Cookies.Delete("accessToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/"
            });

            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/"
            });
        }
    }
}