using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Settings.Domain.Entities
{
    /// <summary>
    /// User preferences and settings
    /// </summary>
    public class UserSettings : Entity
    {
        public Guid UserId { get; private set; }

        // Notification Settings
        public bool EmailNotificationsEnabled { get; private set; }
        public bool PushNotificationsEnabled { get; private set; }
        public bool NotifyOnChannelMessage { get; private set; }
        public bool NotifyOnDirectMessage { get; private set; }
        public bool NotifyOnMention { get; private set; }
        public bool NotifyOnReaction { get; private set; }

        // Privacy Settings
        public bool ShowOnlineStatus { get; private set; }
        public bool ShowLastSeen { get; private set; }
        public bool ShowReadReceipts { get; private set; }
        public bool AllowDirectMessages { get; private set; }

        // Display Settings
        public string Theme { get; private set; } = "light";
        public string Language { get; private set; } = "en";
        public int MessagePageSize { get; private set; } = 50;

        // EF Core constructor
        private UserSettings() { }

        public UserSettings(Guid userId)
        {
            UserId = userId;

            // Default notification settings - all enabled
            EmailNotificationsEnabled = true;
            PushNotificationsEnabled = true;
            NotifyOnChannelMessage = true;
            NotifyOnDirectMessage = true;
            NotifyOnMention = true;
            NotifyOnReaction = true;

            // Default privacy settings
            ShowOnlineStatus = true;
            ShowLastSeen = true;
            ShowReadReceipts = true;
            AllowDirectMessages = true;

            // Default display settings
            Theme = "light";
            Language = "en";
            MessagePageSize = 50;
        }

        public void UpdateNotificationSettings(
            bool emailEnabled,
            bool pushEnabled,
            bool channelMessages,
            bool directMessages,
            bool mentions,
            bool reactions)
        {
            EmailNotificationsEnabled = emailEnabled;
            PushNotificationsEnabled = pushEnabled;
            NotifyOnChannelMessage = channelMessages;
            NotifyOnDirectMessage = directMessages;
            NotifyOnMention = mentions;
            NotifyOnReaction = reactions;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void UpdatePrivacySettings(
            bool showOnlineStatus,
            bool showLastSeen,
            bool showReadReceipts,
            bool allowDirectMessages)
        {
            ShowOnlineStatus = showOnlineStatus;
            ShowLastSeen = showLastSeen;
            ShowReadReceipts = showReadReceipts;
            AllowDirectMessages = allowDirectMessages;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void UpdateDisplaySettings(
            string theme,
            string language,
            int messagePageSize)
        {
            if (messagePageSize < 10 || messagePageSize > 100)
                throw new ArgumentException("Message page size must be between 10 and 100", nameof(messagePageSize));

            Theme = theme;
            Language = language;
            MessagePageSize = messagePageSize;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}