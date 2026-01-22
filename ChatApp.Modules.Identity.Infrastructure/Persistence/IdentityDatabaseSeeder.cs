using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Enums;
using ChatApp.Modules.Identity.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence
{
    /// <summary>
    /// Handles seeding of initial data into the database
    /// This runs automatically when the application starts if the database is empty
    /// </summary>
    public static class IdentityDatabaseSeeder
    {
        /// <summary>
        /// Seeds the database with initial data if it's empty
        /// This is idempotent - it checks before adding data so it's safe to run multiple times
        /// </summary>
        public static async Task SeedAsync(IdentityDbContext context, IPasswordHasher passwordHasher, ILogger logger)
        {
            try
            {
                // Check if we need to seed by looking for existing users
                var hasUsers = await context.Users.AnyAsync();

                if (hasUsers)
                {
                    logger.LogInformation("Database already contains data. Skipping seed operation");
                    return;
                }

                logger.LogInformation("Database is empty. Beginning seed operation...");

                // Start a transaction to ensure all-or-nothing seeding
                await using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    await SeedPositionsAsync(context, logger);
                    await SeedDefaultUsersAsync(context, passwordHasher, logger);

                    // Save all changes to the database
                    await context.SaveChangesAsync();

                    // Commit the transaction - everything succeeded
                    await transaction.CommitAsync();

                    logger.LogInformation("Database seeding completed successfully");
                }
                catch (Exception ex)
                {
                    // Something went wrong - rollback the transaction
                    logger.LogError(ex, "Error during database seeding. Rolling back transaction");
                    await transaction.RollbackAsync();
                    throw; // Re-throw to be caught by the outer handler
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed database");
                throw;
            }
        }

        /// <summary>
        /// Creates the CEO position
        /// </summary>
        private static async Task SeedPositionsAsync(IdentityDbContext context, ILogger logger)
        {
            if (await context.Positions.AnyAsync())
            {
                logger?.LogInformation("Positions already exist, skipping position seeding");
                return;
            }

            logger?.LogInformation("Seeding default positions...");

            // CEO Position (DepartmentId = null)
            var ceoPosition = new Position("CEO", null, "Chief Executive Officer");
            await context.Positions.AddAsync(ceoPosition);

            logger?.LogInformation("Seeded 1 default position:");
            logger?.LogInformation("  - CEO");
        }

        /// <summary>
        /// Creates the initial default users (System Administrator)
        /// These are essential for initial system access
        /// </summary>
        private static async Task SeedDefaultUsersAsync(IdentityDbContext context, IPasswordHasher passwordHasher, ILogger logger)
        {
            if (await context.Users.AnyAsync())
            {
                logger?.LogInformation("Users already exist, skipping default user seeding");
                return;
            }

            logger?.LogInformation("Seeding default users...");

            // 1. System Administrator (no specific position)
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var adminUser = new User(
                firstName: "System",
                lastName: "Administrator",
                email: "admin@chatapp.com",
                passwordHash: passwordHasher.Hash("Yusif2000+"),
                role: Role.Administrator,
                avatarUrl: null,
                aboutMe: "System administrator account created during initial setup",
                dateOfBirth: null,
                workPhone: null,
                hiringDate: DateTime.UtcNow
            )
            {
                Id = adminUserId
            };

            await context.Users.AddAsync(adminUser);

            logger?.LogInformation("Seeded 1 default user:");
            logger?.LogInformation("  - System Administrator: admin@chatapp.com");
            logger?.LogWarning("SECURITY: Remember to change default password after first login!");
        }
    }
}
