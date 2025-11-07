namespace ChatApp.Modules.Settings.Application.DTOs
{
    public record NotificationSettingsDto(
        bool EmailNotificationsEnabled,
        bool PushNotificationsEnabled,
        bool NotifyOnChannelMessage,
        bool NotifyOnDirectMessage,
        bool NotifyOnMention,
        bool NotifyOnReaction
    );
}