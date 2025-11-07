using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Settings.Domain.Events
{
    public record UserSettingsUpdatedEvent(
        Guid UserId,
        string SettingType,
        DateTime UpdatedAtUtc
    ) : DomainEvent;
}