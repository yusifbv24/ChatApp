using ChatApp.Modules.Identity.Application.Commands.Login;
using ChatApp.Modules.Identity.Application.Commands.RefreshToken;
using ChatApp.Modules.Identity.Application.DTOs.Requests;
using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Queries.GetUser;
using ChatApp.Shared.Kernel.Interfaces;
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
        private readonly ISessionStore _sessionStore;

        private const string SessionCookieName = "_sid";

        public AuthController(
            IMediator mediator,
            ILogger<AuthController> logger,
            IConfiguration configuration,
            ISessionStore sessionStore)
        {
            _mediator = mediator;
            _logger = logger;
            _configuration = configuration;
            _sessionStore = sessionStore;
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

            var loginResponse = result.Value!;

            // Parse userId from JWT for session tracking
            var userId = GetUserIdFromLoginResponse(loginResponse);

            // Create opaque session — real tokens stay server-side only
            var accessTokenLifetime = TimeSpan.FromSeconds(loginResponse.ExpiresIn);
            var refreshTokenDays = _configuration.GetValue("JwtSettings:RefreshTokenExpirationDays", 30);
            var refreshTokenLifetime = TimeSpan.FromDays(refreshTokenDays);

            var sessionId = _sessionStore.CreateSession(
                userId,
                loginResponse.AccessToken,
                loginResponse.RefreshToken,
                accessTokenLifetime,
                refreshTokenLifetime);

            // Only opaque session ID goes to the cookie — no tokens exposed
            SetSessionCookie(sessionId, loginResponse.RememberMe, refreshTokenLifetime);

            return Ok(new { success = true, message = "Login successful" });
        }



        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
        {
            var sessionId = Request.Cookies[SessionCookieName];

            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { error = "No session found" });

            // Get refresh token from server-side session store
            var refreshToken = _sessionStore.GetRefreshToken(sessionId);

            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest(new { error = "Session expired" });

            var result = await _mediator.Send(new RefreshTokenCommand(refreshToken), cancellationToken);

            if (result.IsFailure)
            {
                // Session is invalid — clear it
                _sessionStore.RemoveSession(sessionId);
                ClearSessionCookie();
                return BadRequest(new { error = result.Error });
            }

            var refreshResponse = result.Value!;

            // Update session with new tokens — cookie stays the same (same opaque ID)
            var accessTokenLifetime = TimeSpan.FromSeconds(refreshResponse.ExpiresIn);
            var refreshTokenDays = _configuration.GetValue("JwtSettings:RefreshTokenExpirationDays", 30);
            var refreshTokenLifetime = TimeSpan.FromDays(refreshTokenDays);

            _sessionStore.UpdateTokens(sessionId, refreshResponse.AccessToken, refreshResponse.RefreshToken, accessTokenLifetime, refreshTokenLifetime);

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
        /// This endpoint requires authentication via cookie, so it's secure — the token is only returned
        /// to authenticated users for establishing SignalR connections.
        /// </summary>
        [HttpGet("signalr-token")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetSignalRToken()
        {
            var sessionId = Request.Cookies[SessionCookieName];

            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized(new { error = "No session found" });

            var accessToken = _sessionStore.GetAccessToken(sessionId);

            if (string.IsNullOrEmpty(accessToken))
                return Unauthorized(new { error = "Session expired" });

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

            // Remove server-side session
            var sessionId = Request.Cookies[SessionCookieName];
            if (!string.IsNullOrEmpty(sessionId))
            {
                _sessionStore.RemoveSession(sessionId);
            }

            // Clear session cookie
            ClearSessionCookie();

            _logger?.LogInformation("User {UserId} logged out successfully", userId);
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
        /// Extracts user ID from the login response JWT (without full validation, just to get the sub claim)
        /// </summary>
        private static Guid GetUserIdFromLoginResponse(LoginResponse loginResponse)
        {
            try
            {
                // JWT has 3 parts: header.payload.signature
                var parts = loginResponse.AccessToken.Split('.');
                if (parts.Length != 3) return Guid.Empty;

                var payload = parts[1];
                // Fix base64 padding
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var doc = System.Text.Json.JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("nameid", out var nameid) ||
                    doc.RootElement.TryGetProperty("sub", out nameid))
                {
                    if (Guid.TryParse(nameid.GetString(), out var userId))
                        return userId;
                }
            }
            catch { }

            return Guid.Empty;
        }

        /// <summary>
        /// Sets the opaque session cookie (no tokens exposed)
        /// </summary>
        private void SetSessionCookie(string sessionId, bool rememberMe, TimeSpan refreshTokenLifetime)
        {
            var isProduction = !_isDevelopment;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = rememberMe ? DateTimeOffset.UtcNow.Add(refreshTokenLifetime) : null,
                Path = "/"
            };

            Response.Cookies.Append(SessionCookieName, sessionId, cookieOptions);
        }

        /// <summary>
        /// Clears the session cookie on logout
        /// </summary>
        private void ClearSessionCookie()
        {
            var isProduction = !_isDevelopment;

            Response.Cookies.Delete(SessionCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/"
            });
        }
    }
}