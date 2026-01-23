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
                    // Step 1: Seed departments first
                    var departments = await SeedDepartmentsAsync(context, logger);
                    await context.SaveChangesAsync();

                    // Step 2: Seed positions (needs IT department for .NET Developer)
                    var positions = await SeedPositionsAsync(context, departments, logger);
                    await context.SaveChangesAsync();

                    // Step 3: Seed users and employees
                    await SeedDefaultUsersAsync(context, passwordHasher, departments, positions, logger);
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
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed database");
                throw;
            }
        }

        /// <summary>
        /// Creates the default departments
        /// </summary>
        private static async Task<Dictionary<string, Department>> SeedDepartmentsAsync(
            IdentityDbContext context,
            ILogger logger)
        {
            if (await context.Departments.AnyAsync())
            {
                logger?.LogInformation("Departments already exist, skipping department seeding");
                return new Dictionary<string, Department>();
            }

            logger?.LogInformation("Seeding default departments...");

            var departments = new Dictionary<string, Department>
            {
                ["CEO"] = new Department("CEO"),
                ["ASSISTANT OF CEO"] = new Department("ASSISTANT OF CEO"),
                ["IT"] = new Department("IT"),
                ["FINANCE"] = new Department("FINANCE"),
                ["ACCOUNTING"] = new Department("ACCOUNTING")
            };

            foreach (var dept in departments.Values)
            {
                await context.Departments.AddAsync(dept);
            }

            logger?.LogInformation("Seeded {Count} default departments:", departments.Count);
            foreach (var name in departments.Keys)
            {
                logger?.LogInformation("  - {Name}", name);
            }

            return departments;
        }

        /// <summary>
        /// Creates the default positions
        /// </summary>
        private static async Task<Dictionary<string, Position>> SeedPositionsAsync(
            IdentityDbContext context,
            Dictionary<string, Department> departments,
            ILogger logger)
        {
            if (await context.Positions.AnyAsync())
            {
                logger?.LogInformation("Positions already exist, skipping position seeding");
                return new Dictionary<string, Position>();
            }

            logger?.LogInformation("Seeding default positions...");

            // Get IT department from local tracker or database
            var itDepartment = departments.TryGetValue("IT", out var itDept)
                ? itDept
                : context.Departments.Local.FirstOrDefault(d => d.Name == "IT")
                  ?? await context.Departments.FirstOrDefaultAsync(d => d.Name == "IT");

            var positions = new Dictionary<string, Position>
            {
                [".NET Developer"] = new Position(".NET Developer", itDepartment?.Id, ".NET development specialist")
            };

            foreach (var position in positions.Values)
            {
                await context.Positions.AddAsync(position);
            }

            logger?.LogInformation("Seeded {Count} default position(s):", positions.Count);
            foreach (var name in positions.Keys)
            {
                logger?.LogInformation("  - {Name}", name);
            }

            return positions;
        }

        /// <summary>
        /// Creates the initial default users (CEO and System Administrator)
        /// These are essential for initial system access
        /// </summary>
        private static async Task SeedDefaultUsersAsync(
            IdentityDbContext context,
            IPasswordHasher passwordHasher,
            Dictionary<string, Department> departments,
            Dictionary<string, Position> positions,
            ILogger logger)
        {
            if (await context.Users.AnyAsync())
            {
                logger?.LogInformation("Users already exist, skipping default user seeding");
                return;
            }

            logger?.LogInformation("Seeding default users...");

            // Get departments from local tracker or provided dictionary
            var ceoDepartment = departments.TryGetValue("CEO", out var ceoDept)
                ? ceoDept
                : context.Departments.Local.FirstOrDefault(d => d.Name == "CEO")
                  ?? throw new InvalidOperationException("CEO department must be seeded before users");

            var itDepartment = departments.TryGetValue("IT", out var itDept)
                ? itDept
                : context.Departments.Local.FirstOrDefault(d => d.Name == "IT")
                  ?? throw new InvalidOperationException("IT department must be seeded before users");

            // Get .NET Developer position
            var dotNetDevPosition = positions.TryGetValue(".NET Developer", out var pos)
                ? pos
                : context.Positions.Local.FirstOrDefault(p => p.Name == ".NET Developer")
                  ?? throw new InvalidOperationException(".NET Developer position must be seeded before users");

            // ========== 1. CEO User - Aqil Zeynalov ==========
            var ceoUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var ceoUser = new User(
                firstName: "Aqil",
                lastName: "Zeynalov",
                email: "ceo@chatapp.com",
                passwordHash: passwordHasher.Hash("Ceo2000+"),
                role: Role.Administrator,
                avatarUrl: null)
            {
                Id = ceoUserId
            };

            await context.Users.AddAsync(ceoUser);

            // CEO Employee - NO position, assigned to CEO department
            var ceoEmployee = new Employee(
                userId: ceoUserId,
                dateOfBirth: null,
                workPhone: null,
                aboutMe: "Chief Executive Officer",
                hiringDate: DateTime.UtcNow);

            ceoEmployee.AssignToDepartment(ceoDepartment.Id);
            // No position for CEO
            await context.Employees.AddAsync(ceoEmployee);

            // Set CEO as Head of CEO Department
            ceoDepartment.AssignHead(ceoUserId);

            // ========== 2. System Administrator - Yusif Baghiyev ==========
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var adminUser = new User(
                firstName: "Yusif",
                lastName: "Baghiyev",
                email: "admin@chatapp.com",
                passwordHash: passwordHasher.Hash("Yusif2000+"),
                role: Role.Administrator,
                avatarUrl: null)
            {
                Id = adminUserId
            };

            await context.Users.AddAsync(adminUser);

            // Admin Employee - .NET Developer position, IT department
            var adminEmployee = new Employee(
                userId: adminUserId,
                dateOfBirth: new DateTime(2000, 1, 1),
                workPhone: null,
                aboutMe: ".NET Developer and System Administrator",
                hiringDate: DateTime.UtcNow);

            adminEmployee.AssignToDepartment(itDepartment.Id);
            adminEmployee.AssignToPosition(dotNetDevPosition.Id);
            await context.Employees.AddAsync(adminEmployee);

            logger?.LogInformation("Seeded 2 default users with employee records:");
            logger?.LogInformation("  - CEO: Aqil Zeynalov (ceo@chatapp.com) - Department: CEO, Head of Department");
            logger?.LogInformation("  - Admin: Yusif Baghiyev (admin@chatapp.com) - Department: IT, Position: .NET Developer");
            logger?.LogWarning("SECURITY: Remember to change default passwords after first login!");
        }
    }
}
