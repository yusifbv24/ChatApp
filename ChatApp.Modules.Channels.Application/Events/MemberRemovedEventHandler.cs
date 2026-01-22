using ChatApp.Modules.Channels.Domain.Events;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Events
{
    /// <summary>
    /// Handles MemberRemovedEvent - notifies channel members via SignalR when a member leaves
    /// </summary>
    public class MemberRemovedEventHandler(
        ISignalRNotificationService signalRNotificationService,
        ILogger<MemberRemovedEventHandler> logger)
    {
        public async Task HandleAsync(MemberRemovedEvent @event)
        {
            try
            {
                logger?.LogInformation(
                    "Handling MemberRemovedEvent: User {UserId} left channel {ChannelId}",
                    @event.UserId,
                    @event.ChannelId);

                // Get the display name of the user who left
                // (We need to fetch user info - for now use UserId as placeholder)
                // TODO: Add user service reference or include display name in event
                var leftUserDisplayName = @event.UserId.ToString(); // Placeholder

                // Notify channel group via SignalR (only to members who are in the group)
                await signalRNotificationService.NotifyMemberLeftChannelAsync(
                    @event.ChannelId,
                    @event.UserId,
                    leftUserDisplayName);

                logger.LogInformation(
                    "Notified channel {ChannelId} group about user {UserId} leaving",
                    @event.ChannelId,
                    @event.UserId);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error handling MemberRemovedEvent for user {UserId} in channel {ChannelId}",
                    @event.UserId,
                    @event.ChannelId);
                // Don't throw - this is a non-critical operation (notification only)
            }
        }
    }
}