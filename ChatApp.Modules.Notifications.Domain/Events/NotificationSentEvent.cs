using ChatApp.Modules.Notifications.Domain.Enums;
using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Notifications.Domain.Events
{
    public record NotificationSentEvent(
        Guid NotificationId,
        Guid UserId,
        NotificationType Type,
        NotificationChannel Channel,
        DateTime SentAtUtc
    ):DomainEvent;
}