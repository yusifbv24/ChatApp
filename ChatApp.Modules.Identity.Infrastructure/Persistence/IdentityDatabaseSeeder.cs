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
        /// Creates the initial default users (CEO and System Administrator)
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

            // Get CEO position (check local change tracker first, then database)
            var ceoPosition = context.Positions.Local.FirstOrDefault(p => p.Name == "CEO")
                ?? await context.Positions.FirstOrDefaultAsync(p => p.Name == "CEO");
            if (ceoPosition == null)
            {
                logger?.LogError("CEO position not found. Cannot create CEO user.");
                throw new InvalidOperationException("CEO position must be seeded before users");
            }

            // 1. CEO User (Authentication & Basic Profile)
            var ceoUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var ceoUser = new User(
                firstName: "Yusif",
                lastName: "Baghiyev",
                email: "ceo@chatapp.com",
                passwordHash: passwordHasher.Hash("Yusif2000+"),
                role: Role.Administrator,
                avatarUrl: null)
            {
                Id = ceoUserId
            };

            await context.Users.AddAsync(ceoUser);

            // 1.1 CEO Employee (Organizational & Sensitive Data)
            var ceoEmployee = new Employee(
                userId: ceoUserId,
                dateOfBirth: new DateTime(2000, 1, 1),
                workPhone: null,
                aboutMe: "Chief Executive Officer",
                hiringDate: DateTime.UtcNow);

            ceoEmployee.AssignToPosition(ceoPosition.Id);
            await context.Employees.AddAsync(ceoEmployee);

            // 2. System Administrator User
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var adminUser = new User(
                firstName: "System",
                lastName: "Administrator",
                email: "admin@chatapp.com",
                passwordHash: passwordHasher.Hash("Yusif2000+"),
                role: Role.Administrator,
                avatarUrl: null)
            {
                Id = adminUserId
            };

            await context.Users.AddAsync(adminUser);

            // 2.1 Admin Employee (no specific position)
            var adminEmployee = new Employee(
                userId: adminUserId,
                dateOfBirth: null,
                workPhone: null,
                aboutMe: "System administrator account created during initial setup",
                hiringDate: DateTime.UtcNow);

            await context.Employees.AddAsync(adminEmployee);

            logger?.LogInformation("Seeded 2 default users with employee records:");
            logger?.LogInformation("  - CEO: ceo@chatapp.com (Position: CEO)");
            logger?.LogInformation("  - System Administrator: admin@chatapp.com");
            logger?.LogWarning("SECURITY: Remember to change default passwords after first login!");
        }
    }
}