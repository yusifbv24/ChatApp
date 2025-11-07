using ChatApp.Modules.Notifications.Application.DTOs;
using ChatApp.Modules.Notifications.Application.Interfaces;
using ChatApp.Modules.Notifications.Domain.Entities;
using ChatApp.Modules.Notifications.Domain.Enums;
using ChatApp.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Notifications.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationsDbContext _context;

        public NotificationRepository(NotificationsDbContext context)
        {
            _context = context;
        }

        public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(
            Guid userId,
            int pageSize = 50,
            int skip = 0,
            CancellationToken cancellationToken = default)
        {
            var query = from notification in _context.Notifications
                        join sender in _context.Set<UserReadModel>()
                            on notification.SenderId equals sender.Id into senderJoin
                        from sender in senderJoin.DefaultIfEmpty()
                        where notification.UserId == userId
                        orderby notification.CreatedAtUtc descending
                        select new NotificationDto(
                            notification.Id,
                            notification.UserId,
                            notification.Type,
                            notification.Channel,
                            notification.Status,
                            notification.Title,
                            notification.Message,
                            notification.ActionUrl,
                            sender != null ? sender.Id : null,
                            sender != null ? sender.Username : null,
                            sender != null ? sender.DisplayName : null,
                            notification.CreatedAtUtc,
                            notification.SentAtUtc,
                            notification.ReadAtUtc
                        );

            return await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.Status != NotificationStatus.Read)
                .CountAsync(cancellationToken);
        }

        public async Task<List<Notification>> GetPendingNotificationsAsync(
            NotificationChannel channel,
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .Where(n => n.Channel == channel && n.Status == NotificationStatus.Pending)
                .OrderBy(n => n.CreatedAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            await _context.Notifications.AddAsync(notification, cancellationToken);
        }

        public Task DeleteAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            _context.Notifications.Remove(notification);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            _context.Notifications.Update(notification);
            return Task.CompletedTask;
        }

        public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await _context.Notifications
                .Where(n => n.UserId == userId && n.Status == NotificationStatus.Sent)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(n => n.Status, NotificationStatus.Read)
                        .SetProperty(n => n.ReadAtUtc, DateTime.UtcNow)
                        .SetProperty(n => n.UpdatedAtUtc, DateTime.UtcNow),
                    cancellationToken);
        }
    }
}