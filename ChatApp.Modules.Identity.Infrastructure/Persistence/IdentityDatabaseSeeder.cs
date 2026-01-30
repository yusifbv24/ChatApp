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

            // Create parent departments first
            var departments = new Dictionary<string, Department>
            {
                ["Engineering"] = new Department("Engineering"),
                ["Sales & Marketing"] = new Department("Sales & Marketing"),
                ["Finance"] = new Department("Finance"),
                ["HR"] = new Department("HR")
            };

            foreach (var dept in departments.Values)
            {
                await context.Departments.AddAsync(dept);
            }

            // Save parent departments to get their IDs
            await context.SaveChangesAsync();

            // Create subdepartments
            var engineeringDept = departments["Engineering"];
            var salesMarketingDept = departments["Sales & Marketing"];

            var subDepartments = new Dictionary<string, Department>
            {
                ["Frontend Development"] = new Department("Frontend Development", engineeringDept.Id),
                ["Backend Development"] = new Department("Backend Development", engineeringDept.Id),
                ["Sales"] = new Department("Sales", salesMarketingDept.Id)
            };

            foreach (var subDept in subDepartments.Values)
            {
                await context.Departments.AddAsync(subDept);
                departments[subDept.Name] = subDept; // Add to main dictionary
            }

            logger?.LogInformation("Seeded {Count} departments (with subdepartments):", departments.Count);
            logger?.LogInformation("  - Engineering");
            logger?.LogInformation("    - Frontend Development");
            logger?.LogInformation("    - Backend Development");
            logger?.LogInformation("  - Sales & Marketing");
            logger?.LogInformation("    - Sales");
            logger?.LogInformation("  - Finance");
            logger?.LogInformation("  - HR");

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

            // Get departments from local tracker or database
            var frontendDept = departments.TryGetValue("Frontend Development", out var frontDept)
                ? frontDept
                : context.Departments.Local.FirstOrDefault(d => d.Name == "Frontend Development")
                  ?? await context.Departments.FirstOrDefaultAsync(d => d.Name == "Frontend Development");

            var backendDept = departments.TryGetValue("Backend Development", out var backDept)
                ? backDept
                : context.Departments.Local.FirstOrDefault(d => d.Name == "Backend Development")
                  ?? await context.Departments.FirstOrDefaultAsync(d => d.Name == "Backend Development");

            var salesDept = departments.TryGetValue("Sales", out var salesD)
                ? salesD
                : context.Departments.Local.FirstOrDefault(d => d.Name == "Sales")
                  ?? await context.Departments.FirstOrDefaultAsync(d => d.Name == "Sales");

            var financeDept = departments.TryGetValue("Finance", out var finDept)
                ? finDept
                : context.Departments.Local.FirstOrDefault(d => d.Name == "Finance")
                  ?? await context.Departments.FirstOrDefaultAsync(d => d.Name == "Finance");

            var hrDept = departments.TryGetValue("HR", out var hrD)
                ? hrD
                : context.Departments.Local.FirstOrDefault(d => d.Name == "HR")
                  ?? await context.Departments.FirstOrDefaultAsync(d => d.Name == "HR");

            var positions = new Dictionary<string, Position>
            {
                // Engineering positions
                ["Frontend Developer"] = new Position("Frontend Developer", frontendDept?.Id, "Develops user-facing web applications"),
                ["Senior Frontend Developer"] = new Position("Senior Frontend Developer", frontendDept?.Id, "Lead frontend developer"),
                ["Backend Developer"] = new Position("Backend Developer", backendDept?.Id, "Develops server-side applications"),
                ["Senior Backend Developer"] = new Position("Senior Backend Developer", backendDept?.Id, "Lead backend developer"),

                // Sales positions
                ["Sales Manager"] = new Position("Sales Manager", salesDept?.Id, "Manages sales team and strategy"),
                ["Sales Representative"] = new Position("Sales Representative", salesDept?.Id, "Handles client relationships and sales"),

                // Finance positions
                ["Financial Analyst"] = new Position("Financial Analyst", financeDept?.Id, "Analyzes financial data and reports"),
                ["Chief Financial Officer"] = new Position("Chief Financial Officer", financeDept?.Id, "Oversees all financial operations"),

                // HR positions
                ["HR Manager"] = new Position("HR Manager", hrDept?.Id, "Manages human resources department"),
                ["HR Specialist"] = new Position("HR Specialist", hrDept?.Id, "Handles recruitment and employee relations")
            };

            foreach (var position in positions.Values)
            {
                await context.Positions.AddAsync(position);
            }

            logger?.LogInformation("Seeded {Count} default positions", positions.Count);

            return positions;
        }

        /// <summary>
        /// Creates the initial default users distributed across departments
        /// These are essential for initial system access and testing
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

            // Helper function to get department
            Department GetDept(string name) => departments.TryGetValue(name, out var dept) ? dept
                : context.Departments.Local.FirstOrDefault(d => d.Name == name)
                ?? throw new InvalidOperationException($"{name} department must be seeded before users");

            // Helper function to get position
            Position GetPos(string name) => positions.TryGetValue(name, out var pos) ? pos
                : context.Positions.Local.FirstOrDefault(p => p.Name == name)
                ?? throw new InvalidOperationException($"{name} position must be seeded before users");

            // ========== 1. Emily Johnson - CFO ==========
            var user1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var user1 = new User("Emily", "Johnson", "emily.johnson@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.Administrator, null) { Id = user1Id };
            await context.Users.AddAsync(user1);
            var emp1 = new Employee(user1Id, new DateTime(1985, 3, 15), "+1234567890",
                "Chief Financial Officer overseeing all financial operations", DateTime.UtcNow.AddYears(-8));
            emp1.AssignToDepartment(GetDept("Finance").Id);
            emp1.AssignToPosition(GetPos("Chief Financial Officer").Id);
            await context.Employees.AddAsync(emp1);
            GetDept("Finance").AssignHead(user1Id);

            // ========== 2. Michael Chen - Senior Backend Developer ==========
            var user2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var user2 = new User("Michael", "Chen", "michael.chen@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user2Id };
            await context.Users.AddAsync(user2);
            var emp2 = new Employee(user2Id, new DateTime(1990, 7, 22), "+1234567891",
                "Senior Backend Developer specializing in .NET and cloud architecture", DateTime.UtcNow.AddYears(-5));
            emp2.AssignToDepartment(GetDept("Backend Development").Id);
            emp2.AssignToPosition(GetPos("Senior Backend Developer").Id);
            await context.Employees.AddAsync(emp2);
            GetDept("Backend Development").AssignHead(user2Id);

            // ========== 3. Sarah Williams - Senior Frontend Developer ==========
            var user3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var user3 = new User("Sarah", "Williams", "sarah.williams@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user3Id };
            await context.Users.AddAsync(user3);
            var emp3 = new Employee(user3Id, new DateTime(1992, 11, 8), "+1234567892",
                "Senior Frontend Developer expert in React and modern web technologies", DateTime.UtcNow.AddYears(-4));
            emp3.AssignToDepartment(GetDept("Frontend Development").Id);
            emp3.AssignToPosition(GetPos("Senior Frontend Developer").Id);
            await context.Employees.AddAsync(emp3);
            GetDept("Frontend Development").AssignHead(user3Id);

            // ========== 4. David Martinez - Backend Developer ==========
            var user4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var user4 = new User("David", "Martinez", "david.martinez@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user4Id };
            await context.Users.AddAsync(user4);
            var emp4 = new Employee(user4Id, new DateTime(1995, 5, 18), "+1234567893",
                "Backend Developer working on microservices and APIs", DateTime.UtcNow.AddYears(-2));
            emp4.AssignToDepartment(GetDept("Backend Development").Id);
            emp4.AssignToPosition(GetPos("Backend Developer").Id);
            await context.Employees.AddAsync(emp4);

            // ========== 5. Jessica Brown - Frontend Developer ==========
            var user5Id = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var user5 = new User("Jessica", "Brown", "jessica.brown@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user5Id };
            await context.Users.AddAsync(user5);
            var emp5 = new Employee(user5Id, new DateTime(1994, 9, 25), "+1234567894",
                "Frontend Developer focused on UI/UX and responsive design", DateTime.UtcNow.AddYears(-3));
            emp5.AssignToDepartment(GetDept("Frontend Development").Id);
            emp5.AssignToPosition(GetPos("Frontend Developer").Id);
            await context.Employees.AddAsync(emp5);

            // ========== 6. Robert Taylor - Sales Manager ==========
            var user6Id = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var user6 = new User("Robert", "Taylor", "robert.taylor@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user6Id };
            await context.Users.AddAsync(user6);
            var emp6 = new Employee(user6Id, new DateTime(1988, 2, 14), "+1234567895",
                "Sales Manager leading the sales team and driving revenue growth", DateTime.UtcNow.AddYears(-6));
            emp6.AssignToDepartment(GetDept("Sales").Id);
            emp6.AssignToPosition(GetPos("Sales Manager").Id);
            await context.Employees.AddAsync(emp6);
            GetDept("Sales").AssignHead(user6Id);
            GetDept("Sales & Marketing").AssignHead(user6Id);

            // ========== 7. Jennifer Wilson - Sales Representative ==========
            var user7Id = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var user7 = new User("Jennifer", "Wilson", "jennifer.wilson@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user7Id };
            await context.Users.AddAsync(user7);
            var emp7 = new Employee(user7Id, new DateTime(1993, 6, 30), "+1234567896",
                "Sales Representative handling enterprise client relationships", DateTime.UtcNow.AddYears(-2));
            emp7.AssignToDepartment(GetDept("Sales").Id);
            emp7.AssignToPosition(GetPos("Sales Representative").Id);
            await context.Employees.AddAsync(emp7);

            // ========== 8. James Anderson - Financial Analyst ==========
            var user8Id = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var user8 = new User("James", "Anderson", "james.anderson@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user8Id };
            await context.Users.AddAsync(user8);
            var emp8 = new Employee(user8Id, new DateTime(1991, 12, 5), "+1234567897",
                "Financial Analyst providing detailed financial reports and analysis", DateTime.UtcNow.AddYears(-4));
            emp8.AssignToDepartment(GetDept("Finance").Id);
            emp8.AssignToPosition(GetPos("Financial Analyst").Id);
            await context.Employees.AddAsync(emp8);

            // ========== 9. Linda Garcia - HR Manager ==========
            var user9Id = Guid.Parse("00000000-0000-0000-0000-000000000009");
            var user9 = new User("Linda", "Garcia", "linda.garcia@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user9Id };
            await context.Users.AddAsync(user9);
            var emp9 = new Employee(user9Id, new DateTime(1987, 4, 20), "+1234567898",
                "HR Manager overseeing recruitment, employee relations, and HR policies", DateTime.UtcNow.AddYears(-7));
            emp9.AssignToDepartment(GetDept("HR").Id);
            emp9.AssignToPosition(GetPos("HR Manager").Id);
            await context.Employees.AddAsync(emp9);
            GetDept("HR").AssignHead(user9Id);

            // ========== 10. Christopher Lee - HR Specialist ==========
            var user10Id = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var user10 = new User("Christopher", "Lee", "christopher.lee@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user10Id };
            await context.Users.AddAsync(user10);
            var emp10 = new Employee(user10Id, new DateTime(1996, 8, 12), "+1234567899",
                "HR Specialist handling recruitment and onboarding processes", DateTime.UtcNow.AddYears(-1));
            emp10.AssignToDepartment(GetDept("HR").Id);
            emp10.AssignToPosition(GetPos("HR Specialist").Id);
            await context.Employees.AddAsync(emp10);

            // Set Engineering department head
            GetDept("Engineering").AssignHead(user2Id);

            logger?.LogInformation("Seeded 10 default users with employee records distributed across 4 main departments");
            logger?.LogWarning("SECURITY: All users have password 'Password123!' - Remember to change after first login!");
        }
    }
}
