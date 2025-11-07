using ChatApp.Modules.Notifications.Application.DTOs;
using ChatApp.Modules.Notifications.Application.Interfaces;
using ChatApp.Modules.Notifications.Domain.Enums;
using ChatApp.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Notifications.Infrastructure.BackgroundServices
{
    public class EmailNotificationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailNotificationWorker> _logger;

        public EmailNotificationWorker(
            IServiceProvider serviceProvider,
            ILogger<EmailNotificationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Notification Worker starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingEmailNotificationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email notifications");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }

            _logger.LogInformation("Email Notification Worker stopping");
        }

        private async Task ProcessPendingEmailNotificationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var pendingNotifications = await unitOfWork.Notifications.GetPendingNotificationsAsync(
                NotificationChannel.Email,
                batchSize: 50,
                cancellationToken);

            if (pendingNotifications.Count == 0)
                return;

            _logger.LogInformation("Processing {Count} pending email notifications", pendingNotifications.Count);

            foreach (var notification in pendingNotifications)
            {
                try
                {
                    var user = await GetUserEmailAsync(unitOfWork, notification.UserId, cancellationToken);

                    if (string.IsNullOrEmpty(user.Email))
                    {
                        _logger.LogWarning("User {UserId} has no email address", notification.UserId);
                        notification.MarkAsFailed("User has no email address");
                        await unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
                        continue;
                    }

                    var htmlBody = BuildEmailBody(notification.Title, notification.Message, notification.ActionUrl);

                    var sent = await emailService.SendEmailAsync(
                        user.Email,
                        user.DisplayName,
                        notification.Title,
                        htmlBody,
                        cancellationToken);

                    if (sent)
                    {
                        notification.MarkAsSent();
                        _logger.LogInformation("Email notification {NotificationId} sent to {Email}",
                            notification.Id, user.Email);
                    }
                    else
                    {
                        notification.MarkAsFailed("Failed to send email");
                        _logger.LogWarning("Failed to send email notification {NotificationId}", notification.Id);
                    }

                    await unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending email notification {NotificationId}", notification.Id);
                    notification.MarkAsFailed(ex.Message);
                    await unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Finished processing email notifications");
        }

        private async Task<(string Email, string DisplayName)> GetUserEmailAsync(
            IUnitOfWork unitOfWork,
            Guid userId,
            CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

            var user = await context.Set<UserReadModel>()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            return (user?.Email ?? "", user?.DisplayName ?? "User");
        }

        private string BuildEmailBody(string title, string message, string? actionUrl)
        {
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .button {{ 
            display: inline-block; 
            padding: 12px 24px; 
            background-color: #4F46E5; 
            color: white; 
            text-decoration: none; 
            border-radius: 5px; 
            margin-top: 20px;
        }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>ChatApp</h1>
        </div>
        <div class='content'>
            <h2>{title}</h2>
            <p>{message}</p>
            {(string.IsNullOrEmpty(actionUrl) ? "" : $"<a href='{actionUrl}' class='button'>View Message</a>")}
        </div>
        <div class='footer'>
            <p>You received this email because you have notifications enabled for ChatApp.</p>
            <p>To change your notification preferences, visit your account settings.</p>
        </div>
    </div>
</body>
</html>";

            return htmlBody;
        }
    }
}