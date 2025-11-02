using ChatApp.Modules.Channels.Domain.Entities;
using ChatApp.Modules.Channels.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence
{
    /// <summary>
    /// Seeds initial channels data for the application
    /// </summary>
    public static class ChannelDatabaseSeeder
    {
        /// <summary>
        /// Seeds the database with initial channels if empty
        /// This is idempotent - safe to run multiple times
        /// </summary>
        public static async Task SeedAsync(ChannelsDbContext context, ILogger logger)
        {
            try
            {
                // Check if we need to seed
                var hasChannels = await context.Channels.AnyAsync();

                if (hasChannels)
                {
                    logger.LogInformation("Channels database already contains data. Skipping seed operation");
                    return;
                }

                logger.LogInformation("Channels database is empty. Beginning seed operation...");

                await using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    await SeedDefaultChannelsAsync(context, logger);

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    logger.LogInformation("Channels database seeding completed successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during channels database seeding. Rolling back transaction");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed channels database");
                throw;
            }
        }

        /// <summary>
        /// Seeds default channels for the company
        /// </summary>
        private static async Task SeedDefaultChannelsAsync(ChannelsDbContext context, ILogger logger)
        {
            logger.LogInformation("Seeding default channels...");

            // Get the admin user ID (from Identity module seed)
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var defaultChannels = new[]
            {
                // General company-wide channel
                new Channel(
                    "General",
                    "Company-wide announcements and general discussions",
                    ChannelType.Public,
                    adminUserId)
                { Id = Guid.Parse("10000000-0000-0000-0000-000000000001") },

                // Random channel for casual conversations
                new Channel(
                    "Random",
                    "Off-topic conversations and casual chat",
                    ChannelType.Public,
                    adminUserId)
                { Id = Guid.Parse("10000000-0000-0000-0000-000000000002") },

                // IT Support channel
                new Channel(
                    "IT Support",
                    "Technical support and IT-related questions",
                    ChannelType.Public,
                    adminUserId)
                { Id = Guid.Parse("10000000-0000-0000-0000-000000000003") },

                // HR channel
                new Channel(
                    "Human Resources",
                    "HR announcements, policies, and employee resources",
                    ChannelType.Public,
                    adminUserId)
                { Id = Guid.Parse("10000000-0000-0000-0000-000000000004") },

                // Development team private channel
                new Channel(
                    "Development Team",
                    "Private channel for development team discussions",
                    ChannelType.Private,
                    adminUserId)
                { Id = Guid.Parse("10000000-0000-0000-0000-000000000005") },
            };

            await context.Channels.AddRangeAsync(defaultChannels);
            logger.LogInformation("Seeded {Count} default channels", defaultChannels.Length);
        }
    }
}