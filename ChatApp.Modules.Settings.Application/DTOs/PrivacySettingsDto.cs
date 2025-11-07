namespace ChatApp.Modules.Settings.Application.DTOs
{
    public record PrivacySettingsDto(
        bool ShowOnlineStatus,
        bool ShowLastSeen,
        bool ShowReadReceipts,
        bool AllowDirectMessages
    );
}