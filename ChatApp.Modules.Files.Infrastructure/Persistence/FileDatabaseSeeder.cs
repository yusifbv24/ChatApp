using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Files.Infrastructure.Persistence
{
    public static class FileDatabaseSeeder
    {
        public static async Task SeedAsync(FilesDbContext context, ILogger logger)
        {
            try
            {
                var hasFiles = await context.FileMetadata.AnyAsync();

                if (hasFiles)
                {
                    logger.LogInformation("Files database already contains data. Skipping seed operation");
                    return;
                }

                logger.LogInformation("Files database is empty. No seed data needed for files module.");

                // Files are uploaded dynamically by users, no seed data needed
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed Files database");
                throw;
            }
        }
    }
}