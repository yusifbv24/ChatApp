using ChatApp.Modules.Notifications.Domain.Enums;
using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Notifications.Domain.Entities
{
    public class Notification:Entity
    {
        public Guid UserId { get; private set; }
        public NotificationType Type { get; private set; }
        public NotificationChannel Channel { get; private set; }
        public NotificationStatus Status { get; private set; }

        public string Title { get; private set; }=string.Empty;
        public string Message { get;private set;  }=string.Empty;
        public string? ActionUrl { get; private set; }

        // Source information
        public Guid? SourceId { get; private set;  } // Message ID, Channel ID
        public Guid? SenderId { get; private set; } // Who triggered this notification

        public DateTime? SentAtUtc { get; private set; }
        public DateTime? ReadAtUtc { get; private set; }
        public string? ErrorMessage { get; private set; }
        public int RetryCount { get; private set; }

        private Notification() { }

        public Notification(
            Guid userId,
            NotificationType type,
            NotificationChannel channel,
            string title,
            string message,
            string? actionUrl=null,
            Guid? sourceId=null,
            Guid? senderId=null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty", nameof(title));

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));

            UserId= userId;
            Type= type;
            Channel= channel;
            Status=NotificationStatus.Pending;
            Title= title;
            Message= message;
            ActionUrl= actionUrl;
            SourceId= sourceId;
            SenderId= senderId;
            RetryCount = 0;
        }


        public void MarkAsSent()
        {
            Status = NotificationStatus.Sent;
            SentAtUtc = DateTime.UtcNow;
            UpdatedAtUtc = DateTime.UtcNow;
        }


        public void MarkAsFailed(string errorMessage)
        {
            Status= NotificationStatus.Failed;
            ErrorMessage = errorMessage;
            RetryCount++;
            UpdatedAtUtc= DateTime.UtcNow;
        }


        public void MarkAsRead()
        {
            if (Status == NotificationStatus.Sent)
            {
                Status = NotificationStatus.Read;
                ReadAtUtc= DateTime.UtcNow;
                UpdatedAtUtc=DateTime.UtcNow;
            }
        }

        public bool CanRetry() => Status == NotificationStatus.Failed && RetryCount < 3;
    }
}