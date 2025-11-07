using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Settings.Infrastructure.Persistence
{
    public static class UserSettingsDatabaseSeeder
    {
        public static async Task SeedAsync(SettingsDbContext context, ILogger logger)
        {
            try
            {
                var hasSettings = await context.UserSettings.AnyAsync();

                if (hasSettings)
                {
                    logger.LogInformation("Settings database already contains data. Skipping seed operation");
                    return;
                }

                logger.LogInformation("Settings database is empty. Settings are created per-user on demand.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed Settings database");
                throw;
            }
        }
    }
}