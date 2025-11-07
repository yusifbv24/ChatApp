namespace ChatApp.Modules.Settings.Application.DTOs
{
    public record UserSettingsDto(
        Guid UserId,

        // Notification Settings
        bool EmailNotificationsEnabled,
        bool PushNotificationsEnabled,
        bool NotifyOnChannelMessage,
        bool NotifyOnDirectMessage,
        bool NotifyOnMention,
        bool NotifyOnReaction,

        // Privacy Settings
        bool ShowOnlineStatus,
        bool ShowLastSeen,
        bool ShowReadReceipts,
        bool AllowDirectMessages,

        // Display Settings
        string Theme,
        string Language,
        int MessagePageSize,

        DateTime UpdatedAtUtc
    );
}