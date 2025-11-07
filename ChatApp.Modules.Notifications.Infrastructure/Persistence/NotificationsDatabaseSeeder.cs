using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Notifications.Infrastructure.Persistence
{
    public static class NotificationsDatabaseSeeder
    {
        public static async Task SeedAsync(NotificationsDbContext context, ILogger logger)
        {
            try
            {
                var hasNotifications = await context.Notifications.AnyAsync();

                if (hasNotifications)
                {
                    logger.LogInformation("Notifications database already contains data. Skipping seed operation");
                    return;
                }

                logger.LogInformation("Notifications database is empty. No seed data needed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed Notifications database");
                throw;
            }
        }
    }
}