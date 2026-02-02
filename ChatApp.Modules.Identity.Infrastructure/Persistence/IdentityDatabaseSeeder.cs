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
                    // Step 1: Seed company first
                    var company = await SeedCompanyAsync(context, logger);
                    await context.SaveChangesAsync();

                    // Step 2: Seed departments
                    var departments = await SeedDepartmentsAsync(context, company, logger);
                    await context.SaveChangesAsync();

                    // Step 3: Seed positions (needs departments)
                    var positions = await SeedPositionsAsync(context, departments, logger);
                    await context.SaveChangesAsync();

                    // Step 4: Seed users and employees (includes Head of Company)
                    await SeedDefaultUsersAsync(context, passwordHasher, company, departments, positions, logger);
                    await context.SaveChangesAsync();

                    // Step 5: Assign supervisors (all employees now exist in DB)
                    await AssignSupervisorsAsync(context, logger);
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
        /// Creates the default company
        /// </summary>
        private static async Task<Company> SeedCompanyAsync(
            IdentityDbContext context,
            ILogger logger)
        {
            if (await context.Companies.AnyAsync())
            {
                logger?.LogInformation("Companies already exist, skipping company seeding");
                return await context.Companies.FirstAsync();
            }

            logger?.LogInformation("Seeding default company...");

            var company = new Company("166 Logistics");
            await context.Companies.AddAsync(company);

            logger?.LogInformation("Seeded company: 166 Logistics");

            return company;
        }

        /// <summary>
        /// Creates the default departments
        /// </summary>
        private static async Task<Dictionary<string, Department>> SeedDepartmentsAsync(
            IdentityDbContext context,
            Company company,
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
                ["Engineering"] = new Department("Engineering", company.Id),
                ["Sales & Marketing"] = new Department("Sales & Marketing", company.Id),
                ["Finance"] = new Department("Finance", company.Id),
                ["HR"] = new Department("HR", company.Id)
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
                ["Frontend Development"] = new Department("Frontend Development", company.Id, engineeringDept.Id),
                ["Backend Development"] = new Department("Backend Development", company.Id, engineeringDept.Id),
                ["Sales"] = new Department("Sales", company.Id, salesMarketingDept.Id)
            };

            foreach (var subDept in subDepartments.Values)
            {
                await context.Departments.AddAsync(subDept);
                departments[subDept.Name] = subDept;
            }

            logger?.LogInformation("Seeded {Count} departments (with subdepartments):", departments.Count);

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
                return [];
            }

            logger?.LogInformation("Seeding default positions...");

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
                ["Frontend Developer"] = new Position("Frontend Developer", frontendDept?.Id, "Develops user-facing web applications"),
                ["Senior Frontend Developer"] = new Position("Senior Frontend Developer", frontendDept?.Id, "Lead frontend developer"),
                ["Backend Developer"] = new Position("Backend Developer", backendDept?.Id, "Develops server-side applications"),
                ["Senior Backend Developer"] = new Position("Senior Backend Developer", backendDept?.Id, "Lead backend developer"),
                ["Sales Manager"] = new Position("Sales Manager", salesDept?.Id, "Manages sales team and strategy"),
                ["Sales Representative"] = new Position("Sales Representative", salesDept?.Id, "Handles client relationships and sales"),
                ["Financial Analyst"] = new Position("Financial Analyst", financeDept?.Id, "Analyzes financial data and reports"),
                ["Chief Financial Officer"] = new Position("Chief Financial Officer", financeDept?.Id, "Oversees all financial operations"),
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
        /// Includes Head of Company (Aqil Zeynalov)
        /// </summary>
        private static async Task SeedDefaultUsersAsync(
            IdentityDbContext context,
            IPasswordHasher passwordHasher,
            Company company,
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

            Department GetDept(string name) => departments.TryGetValue(name, out var dept) ? dept
                : context.Departments.Local.FirstOrDefault(d => d.Name == name)
                ?? throw new InvalidOperationException($"{name} department must be seeded before users");

            Position GetPos(string name) => positions.TryGetValue(name, out var pos) ? pos
                : context.Positions.Local.FirstOrDefault(p => p.Name == name)
                ?? throw new InvalidOperationException($"{name} position must be seeded before users");

            // ========== 0. Aqil Zeynalov - Head of Company ==========
            var headOfCompanyId = Guid.Parse("00000000-0000-0000-0000-000000000100");
            var headOfCompanyUser = new User("Aqil", "Zeynalov", "aqil.zeynalov@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.Administrator, null) { Id = headOfCompanyId };
            await context.Users.AddAsync(headOfCompanyUser);
            var headOfCompanyEmp = new Employee(headOfCompanyId, new DateTime(1980, 1, 15), "+994501000000",
                "Head of Company - 166 Logistics", DateTime.UtcNow.AddYears(-15));
            await context.Employees.AddAsync(headOfCompanyEmp);
            company.AssignHead(headOfCompanyId);

            // ========== 1. Leyla Məmmədova - CFO ==========
            var user1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var user1 = new User("Leyla", "Məmmədova", "leyla.mammadova@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.Administrator, null) { Id = user1Id };
            await context.Users.AddAsync(user1);
            var emp1 = new Employee(user1Id, new DateTime(1985, 3, 15), "+994501234567",
                "Chief Financial Officer overseeing all financial operations", DateTime.UtcNow.AddYears(-8));
            emp1.AssignToDepartment(GetDept("Finance").Id);
            emp1.AssignToPosition(GetPos("Chief Financial Officer").Id);
            await context.Employees.AddAsync(emp1);
            GetDept("Finance").AssignHead(user1Id);

            // ========== 2. Rəşad Əliyev - Engineering Head ==========
            var user2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var user2 = new User("Rəşad", "Əliyev", "reshad.aliyev@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user2Id };
            await context.Users.AddAsync(user2);
            var emp2 = new Employee(user2Id, new DateTime(1990, 7, 22), "+994502345678",
                "Engineering Department Head specializing in .NET and cloud architecture", DateTime.UtcNow.AddYears(-5));
            emp2.AssignToDepartment(GetDept("Engineering").Id);
            emp2.AssignToPosition(GetPos("Senior Backend Developer").Id);
            await context.Employees.AddAsync(emp2);

            // ========== 3. Aysel Həsənova - Frontend Development Head ==========
            var user3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var user3 = new User("Aysel", "Həsənova", "aysel.hasanova@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user3Id };
            await context.Users.AddAsync(user3);
            var emp3 = new Employee(user3Id, new DateTime(1992, 11, 8), "+994503456789",
                "Frontend Development Head expert in React and modern web technologies", DateTime.UtcNow.AddYears(-4));
            emp3.AssignToDepartment(GetDept("Frontend Development").Id);
            emp3.AssignToPosition(GetPos("Senior Frontend Developer").Id);
            await context.Employees.AddAsync(emp3);
            GetDept("Frontend Development").AssignHead(user3Id);

            // ========== 4. Elvin Quliyev - Backend Developer ==========
            var user4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var user4 = new User("Elvin", "Quliyev", "elvin.guliyev@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user4Id };
            await context.Users.AddAsync(user4);
            var emp4 = new Employee(user4Id, new DateTime(1995, 5, 18), "+994504567890",
                "Backend Developer working on microservices and APIs", DateTime.UtcNow.AddYears(-2));
            emp4.AssignToDepartment(GetDept("Backend Development").Id);
            emp4.AssignToPosition(GetPos("Backend Developer").Id);
            await context.Employees.AddAsync(emp4);

            // ========== 5. Günel İbrahimova - Frontend Developer ==========
            var user5Id = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var user5 = new User("Günel", "İbrahimova", "gunel.ibrahimova@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user5Id };
            await context.Users.AddAsync(user5);
            var emp5 = new Employee(user5Id, new DateTime(1994, 9, 25), "+994505678901",
                "Frontend Developer focused on UI/UX and responsive design", DateTime.UtcNow.AddYears(-3));
            emp5.AssignToDepartment(GetDept("Frontend Development").Id);
            emp5.AssignToPosition(GetPos("Frontend Developer").Id);
            await context.Employees.AddAsync(emp5);

            // ========== 6. Fərid Musayev - Sales & Marketing Head ==========
            var user6Id = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var user6 = new User("Fərid", "Musayev", "farid.musayev@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user6Id };
            await context.Users.AddAsync(user6);
            var emp6 = new Employee(user6Id, new DateTime(1988, 2, 14), "+994506789012",
                "Sales & Marketing Department Head leading the sales team and driving revenue growth", DateTime.UtcNow.AddYears(-6));
            emp6.AssignToDepartment(GetDept("Sales & Marketing").Id);
            emp6.AssignToPosition(GetPos("Sales Manager").Id);
            await context.Employees.AddAsync(emp6);
            GetDept("Sales").AssignHead(user6Id);
            GetDept("Sales & Marketing").AssignHead(user6Id);

            // ========== 7. Nigar Əhmədova - Sales Representative ==========
            var user7Id = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var user7 = new User("Nigar", "Əhmədova", "nigar.ahmadova@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user7Id };
            await context.Users.AddAsync(user7);
            var emp7 = new Employee(user7Id, new DateTime(1993, 6, 30), "+994507890123",
                "Sales Representative handling enterprise client relationships", DateTime.UtcNow.AddYears(-2));
            emp7.AssignToDepartment(GetDept("Sales").Id);
            emp7.AssignToPosition(GetPos("Sales Representative").Id);
            await context.Employees.AddAsync(emp7);

            // ========== 8. Kamran Abdullayev - Financial Analyst ==========
            var user8Id = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var user8 = new User("Kamran", "Abdullayev", "kamran.abdullayev@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user8Id };
            await context.Users.AddAsync(user8);
            var emp8 = new Employee(user8Id, new DateTime(1991, 12, 5), "+994508901234",
                "Financial Analyst providing detailed financial reports and analysis", DateTime.UtcNow.AddYears(-4));
            emp8.AssignToDepartment(GetDept("Finance").Id);
            emp8.AssignToPosition(GetPos("Financial Analyst").Id);
            await context.Employees.AddAsync(emp8);

            // ========== 9. Sevda Əsgərova - HR Manager ==========
            var user9Id = Guid.Parse("00000000-0000-0000-0000-000000000009");
            var user9 = new User("Sevda", "Əsgərova", "sevda.asgarova@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user9Id };
            await context.Users.AddAsync(user9);
            var emp9 = new Employee(user9Id, new DateTime(1987, 4, 20), "+994509012345",
                "HR Manager overseeing recruitment, employee relations, and HR policies", DateTime.UtcNow.AddYears(-7));
            emp9.AssignToDepartment(GetDept("HR").Id);
            emp9.AssignToPosition(GetPos("HR Manager").Id);
            await context.Employees.AddAsync(emp9);
            GetDept("HR").AssignHead(user9Id);

            // ========== 10. Tural Məhəmmədov - HR Specialist ==========
            var user10Id = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var user10 = new User("Tural", "Məhəmmədov", "tural.mahammadov@chatapp.com",
                passwordHasher.Hash("Password123!"), Role.User, null) { Id = user10Id };
            await context.Users.AddAsync(user10);
            var emp10 = new Employee(user10Id, new DateTime(1996, 8, 12), "+994510123456",
                "HR Specialist handling recruitment and onboarding processes", DateTime.UtcNow.AddYears(-1));
            emp10.AssignToDepartment(GetDept("HR").Id);
            emp10.AssignToPosition(GetPos("HR Specialist").Id);
            await context.Employees.AddAsync(emp10);


            // ========== 11. System Administrator ==========
            var user11Id = Guid.Parse("00000000-0000-0000-0000-000000000011");
            var user11 = new User("System", "Administrator", "admin@chatapp.com",
                passwordHasher.Hash("Yusif2000+"), Role.Administrator, null)
            { Id = user11Id };
            user11.SetSuperAdmin(); // System Administrator is the Super Admin
            await context.Users.AddAsync(user11);
            var emp11 = new Employee(user11Id, new DateTime(2000, 6, 23), "++994708074624",
                "System Administration", DateTime.UtcNow.AddYears(-4));
            emp11.AssignToDepartment(GetDept("Backend Development").Id);
            emp11.AssignToPosition(GetPos("Backend Developer").Id);
            await context.Employees.AddAsync(emp11);

            // Set Engineering department head
            GetDept("Engineering").AssignHead(user2Id);

            logger?.LogInformation("Seeded 11 default users (including Head of Company) with employee records");
            logger?.LogWarning("SECURITY: All users have password 'Password123!' - Remember to change after first login!");
        }

        /// <summary>
        /// Assigns supervisors after all employees are persisted in the database.
        /// This must run as a separate step to avoid self-referencing FK violations.
        /// </summary>
        private static async Task AssignSupervisorsAsync(IdentityDbContext context, ILogger logger)
        {
            logger?.LogInformation("Assigning supervisors...");

            var headOfCompanyId = Guid.Parse("00000000-0000-0000-0000-000000000100");
            var user1Id = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Leyla - Finance head
            var user2Id = Guid.Parse("00000000-0000-0000-0000-000000000002"); // Rəşad - Engineering/Backend head
            var user3Id = Guid.Parse("00000000-0000-0000-0000-000000000003"); // Aysel - Frontend head
            var user6Id = Guid.Parse("00000000-0000-0000-0000-000000000006"); // Fərid - Sales head
            var user9Id = Guid.Parse("00000000-0000-0000-0000-000000000009"); // Sevda - HR head

            // Load all employees from DB
            var employees = await context.Employees.ToListAsync();
            var getEmp = (Guid userId) => employees.First(e => e.UserId == userId);
            // SupervisorId references Employee.Id (PK), not UserId — so we need the Employee.Id
            var getEmpId = (Guid userId) => getEmp(userId).Id;

            var headEmpId = getEmpId(headOfCompanyId);
            var emp1EmpId = getEmpId(user1Id);   // Finance head
            var emp2EmpId = getEmpId(user2Id);   // Engineering/Backend head
            var emp3EmpId = getEmpId(user3Id);   // Frontend head
            var emp6EmpId = getEmpId(user6Id);   // Sales head
            var emp9EmpId = getEmpId(user9Id);   // HR head

            // Department heads → Head of Company
            getEmp(user1Id).AssignSupervisor(headEmpId);   // Finance head
            getEmp(user2Id).AssignSupervisor(headEmpId);   // Engineering head
            getEmp(user6Id).AssignSupervisor(headEmpId);   // Sales head
            getEmp(user9Id).AssignSupervisor(headEmpId);   // HR head

            // Sub-department heads → parent department head
            getEmp(user3Id).AssignSupervisor(emp2EmpId);           // Frontend head → Engineering head

            // Regular employees → their department head
            getEmp(Guid.Parse("00000000-0000-0000-0000-000000000004")).AssignSupervisor(emp2EmpId);   // Elvin → Backend head
            getEmp(Guid.Parse("00000000-0000-0000-0000-000000000005")).AssignSupervisor(emp3EmpId);   // Günel → Frontend head
            getEmp(Guid.Parse("00000000-0000-0000-0000-000000000007")).AssignSupervisor(emp6EmpId);   // Nigar → Sales head
            getEmp(Guid.Parse("00000000-0000-0000-0000-000000000008")).AssignSupervisor(emp1EmpId);   // Kamran → Finance head
            getEmp(Guid.Parse("00000000-0000-0000-0000-000000000010")).AssignSupervisor(emp9EmpId);   // Tural → HR head
            getEmp(Guid.Parse("00000000-0000-0000-0000-000000000011")).AssignSupervisor(emp2EmpId);   // System Admin → Backend head

            logger?.LogInformation("Assigned supervisors for all employees");
        }
    }
}