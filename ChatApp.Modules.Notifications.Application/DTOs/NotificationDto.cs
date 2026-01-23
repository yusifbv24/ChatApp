using ChatApp.Modules.Notifications.Domain.Enums;

namespace ChatApp.Modules.Notifications.Application.DTOs
{
    public record NotificationDto(
        Guid Id,
        Guid UserId,
        NotificationType Type,
        NotificationChannel Channel,
        NotificationStatus Status,
        string Title,
        string Message,
        string? ActionUrl,
        Guid? SenderId,
        string? SenderFullName,
        DateTime CreatedAtUtc,
        DateTime? SentAtUtc,
        DateTime? ReadAtUtc);
}