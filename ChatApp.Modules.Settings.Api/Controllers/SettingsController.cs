using ChatApp.Modules.Settings.Application.Commands.UpdateDisplaySettings;
using ChatApp.Modules.Settings.Application.Commands.UpdateNotificationSettings;
using ChatApp.Modules.Settings.Application.Commands.UpdatePrivacySettings;
using ChatApp.Modules.Settings.Application.DTOs;
using ChatApp.Modules.Settings.Application.Queries.GetUserSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChatApp.Modules.Settings.Api.Controllers
{
    /// <summary>
    /// Controller for managing user settings and preferences
    /// </summary>
    [ApiController]
    [Route("api/settings")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            IMediator mediator,
            ILogger<SettingsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get all settings for current user
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new GetUserSettingsQuery(userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }

        /// <summary>
        /// Update notification settings
        /// </summary>
        [HttpPut("notifications")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateNotificationSettings(
            [FromBody] NotificationSettingsDto request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new UpdateNotificationSettingsCommand(
                    userId,
                    request.EmailNotificationsEnabled,
                    request.PushNotificationsEnabled,
                    request.NotifyOnChannelMessage,
                    request.NotifyOnDirectMessage,
                    request.NotifyOnMention,
                    request.NotifyOnReaction),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Notification settings updated successfully" });
        }

        /// <summary>
        /// Update privacy settings
        /// </summary>
        [HttpPut("privacy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdatePrivacySettings(
            [FromBody] PrivacySettingsDto request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new UpdatePrivacySettingsCommand(
                    userId,
                    request.ShowOnlineStatus,
                    request.ShowLastSeen,
                    request.ShowReadReceipts,
                    request.AllowDirectMessages),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Privacy settings updated successfully" });
        }

        /// <summary>
        /// Update display settings
        /// </summary>
        [HttpPut("display")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateDisplaySettings(
            [FromBody] DisplaySettingsDto request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new UpdateDisplaySettingsCommand(
                    userId,
                    request.Theme,
                    request.Language,
                    request.MessagePageSize),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "Display settings updated successfully" });
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