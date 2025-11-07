using ChatApp.Modules.Notifications.Application.DTOs;
using ChatApp.Modules.Notifications.Domain.Entities;
using ChatApp.Modules.Notifications.Domain.Enums;

namespace ChatApp.Modules.Notifications.Application.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<NotificationDto>> GetUserNotificationsAsync(
            Guid userId,
            int pageSize=50,
            int skip=0,
            CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(Guid userId,CancellationToken cancellationToken = default);

        Task<List<Notification>> GetPendingNotificationsAsync(
            NotificationChannel channel,
            int batchSize = 100,
            CancellationToken cancellationToken = default);

        Task MarkAllAsReadAsync(Guid userId,CancellationToken cancellationToken=default);

        Task AddAsync(Notification notification,CancellationToken cancellationToken = default);
        Task UpdateAsync(Notification notification,CancellationToken cancellationToken = default);
        Task DeleteAsync(Notification notification,CancellationToken cancellationToken = default);
    }
}