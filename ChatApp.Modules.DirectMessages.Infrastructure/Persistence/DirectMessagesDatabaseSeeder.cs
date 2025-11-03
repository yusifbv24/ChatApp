using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence
{
    /// <summary>
    /// Seeds initial direct messages data for the application
    /// </summary>
    public static class DirectMessagesDatabaseSeeder
    {
        /// <summary>
        /// Seeds the database with initial data if empty
        /// This is idempotent - safe to run multiple times
        /// </summary>
        public static async Task SeedAsync(DirectMessagesDbContext context, ILogger logger)
        {
            try
            {
                // Check if we need to seed
                var hasConversations = await context.DirectConversations.AnyAsync();

                if (hasConversations)
                {
                    logger.LogInformation("DirectMessages database already contains data. Skipping seed operation");
                    return;
                }

                logger.LogInformation("DirectMessages database is empty. No seed data needed for direct messages.");

                // Note: Direct messages are created dynamically when users start conversations
                // No initial seed data is required
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed DirectMessages database");
                throw;
            }
        }
    }
}