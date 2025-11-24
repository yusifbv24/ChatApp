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
        public static async Task SeedAsync(IdentityDbContext context,IPasswordHasher passwordHasher, ILogger logger)
        {
            try
            {
                // Check if we need to seed by looking for existing users
                // We check users because they're the most fundamental entity
                var hasUsers = await context.Users.AnyAsync();

                if (hasUsers)
                {
                    logger.LogInformation("Database already contains data. Skipping seed operation");
                    return;
                }

                logger.LogInformation("Database is empty. Beginning seed operation...");

                // Start a transaction to ensure all-or-nothing seeding
                // If anything fails, we rollback everything
                await using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Seed in this order because of foreign key relationships:
                    // 1. Permissions (no dependencies)
                    // 2. Roles (no dependencies)
                    // 3. RolePermissions (depends on Roles and Permissions)
                    // 4. Users (no dependencies for the admin user)
                    // 5. UserRoles (depends on Users and Roles)

                    await SeedPermissionsAsync(context, logger);
                    await SeedRolesAsync(context, logger);
                    await SeedRolePermissionsAsync(context, logger);
                    await SeedAdminUserAsync(context,passwordHasher, logger);

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
        /// Seeds all permissions into the database
        /// Permissions define what actions can be performed in the system
        /// </summary>
        private static async Task SeedPermissionsAsync(IdentityDbContext context, ILogger logger)
        {
            // Check if permissions already exist (from migration seed data)
            if (await context.Permissions.AnyAsync())
            {
                logger.LogInformation("Permissions already exist, skipping permission seeding");
                return;
            }

            logger.LogInformation("Seeding permissions...");

            // Define all permissions for the system
            // Organized by module for clarity
            var permissions = new[]
            {
                // Identity Module - User Management Permissions
                new Permission("Users.Create", "Create new users", "Identity")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111111") },
                new Permission("Users.Read", "View user information", "Identity")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111112") },
                new Permission("Users.Update", "Modify user information", "Identity")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111113") },
                new Permission("Users.Delete", "Delete or deactivate users", "Identity")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111114") },

                // Identity Module - Role Management Permissions
                new Permission("Roles.Create", "Create new roles", "Identity")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111115") },
                new Permission("Roles.Read", "View role information", "Identity")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111116") },
                new Permission("Roles.Update", "Modify role information", "Identity")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111117") },
                new Permission("Roles.Delete", "Delete roles", "Identity")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111118") },

                // Messaging Module - Basic Message Permissions
                new Permission("Messages.Send", "Send messages to other users", "Messaging")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111119") },
                new Permission("Messages.Read", "Read messages", "Messaging")
                    { Id = Guid.Parse("11111111-1111-1111-1111-11111111111a") },
                new Permission("Messages.Edit", "Edit own messages", "Messaging")
                    { Id = Guid.Parse("11111111-1111-1111-1111-11111111111b") },
                new Permission("Messages.Delete", "Delete own messages", "Messaging")
                    { Id = Guid.Parse("11111111-1111-1111-1111-11111111111c") },

                // File Management Permissions
                new Permission("Files.Upload", "Upload files and attachments", "Files")
                    { Id = Guid.Parse("11111111-1111-1111-1111-11111111111d") },
                new Permission("Files.Download", "Download files and attachments", "Files")
                    { Id = Guid.Parse("11111111-1111-1111-1111-11111111111e") },
                new Permission("Files.Delete", "Delete files and attachments", "Files")
                    { Id = Guid.Parse("11111111-1111-1111-1111-11111111111f") },

                // Group/Channel Management Permissions
                new Permission("Groups.Create", "Create groups or channels", "Messaging")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111120") },
                new Permission("Groups.Read","Get groups and it's informations","Messaging")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111123") },
                new Permission("Groups.Manage", "Manage group settings and members", "Messaging")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111121") },
                new Permission("Groups.Delete", "Delete groups or channels", "Messaging")
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111122") },
            };

            await context.Permissions.AddRangeAsync(permissions);
            logger.LogInformation("Seeded {Count} permissions", permissions.Length);
        }

        /// <summary>
        /// Seeds default roles into the database
        /// System roles are marked as non-deletable
        /// </summary>
        private static async Task SeedRolesAsync(IdentityDbContext context, ILogger logger)
        {
            // Check if roles already exist (from migration seed data)
            if (await context.Roles.AnyAsync())
            {
                logger.LogInformation("Roles already exist, skipping role seeding");
                return;
            }

            logger.LogInformation("Seeding roles...");

            var roles = new[]
            {
                // Basic User Role - For regular users of the chat application
                new Role("User", "Standard user with basic messaging capabilities", isSystemRole: true, SystemRole.User)
                    { Id = Guid.Parse("33333333-3333-3333-3333-333333333333") },

                // Operator Role - For users who can manage groups and moderate content
                new Role("Operator", "User with extended permissions for managing groups and content", isSystemRole: true, SystemRole.Operator)
                    { Id = Guid.Parse("22222222-2222-2222-2222-222222222222") },

                // Admin Role - For system administrators with full access
                new Role("Administrator", "Full system access for administrative tasks", isSystemRole: true, SystemRole.Administrator)
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111111") },
            };

            await context.Roles.AddRangeAsync(roles);
            logger.LogInformation("Seeded {Count} roles", roles.Length);
        }


        /// <summary>
        /// Assigns permissions to roles
        /// This defines what each role is allowed to do
        /// </summary>
        private static async Task SeedRolePermissionsAsync(IdentityDbContext context, ILogger logger)
        {
            // Check if role-permission assignments already exist (from migration seed data)
            if (await context.RolePermissions.AnyAsync())
            {
                logger.LogInformation("Role-permission assignments already exist, skipping seeding");
                return;
            }

            logger.LogInformation("Seeding role-permission assignments...");

            // User Role Permissions - Basic functionality for regular users
            var userRolePermissions = new[]
            {
                // Users can view their own and others' basic information
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222221"), // User role
                    Guid.Parse("11111111-1111-1111-1111-111111111112")  // Users.Read
                ) { Id = Guid.NewGuid() },
                
                // Users can send and read messages
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222221"),
                    Guid.Parse("11111111-1111-1111-1111-111111111119")  // Messages.Send
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222221"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111a")  // Messages.Read
                ) { Id = Guid.NewGuid() },
                
                // Users can edit and delete their own messages
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222221"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111b")  // Messages.Edit
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222221"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111c")  // Messages.Delete
                ) { Id = Guid.NewGuid() },
                
                // Users can upload and download files
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222221"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111d")  // Files.Upload
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222221"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111e")  // Files.Download
                ) { Id = Guid.NewGuid() }
            };

            // Operator Role Permissions - Everything users have, plus group management
            var operatorRolePermissions = new[]
            {
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"), // User role
                    Guid.Parse("11111111-1111-1111-1111-111111111112")  // Users.Read
                ) { Id = Guid.NewGuid() },
                
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("11111111-1111-1111-1111-111111111119")  // Messages.Send
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111a")  // Messages.Read
                ) { Id = Guid.NewGuid() },
                
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111b")  // Messages.Edit
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111c")  // Messages.Delete
                ) { Id = Guid.NewGuid() },
                
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111d")  // Files.Upload
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111e")  // Files.Download
                ) { Id = Guid.NewGuid() },

                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111f")  // Files.Delete
                ) { Id = Guid.NewGuid() },

                // Group management capabilities
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("11111111-1111-1111-1111-111111111121")  // Groups.Manage
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("11111111-1111-1111-1111-111111111122")  // Groups.Delete
                ) { Id = Guid.NewGuid() }
            };

            // Administrator Role Permissions - Full access to everything
            var adminRolePermissions = new[]
            {
                // Full user management
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111111")  // Users.Create
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111112")  // Users.Read
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111113")  // Users.Update
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111114")  // Users.Delete
                ) { Id = Guid.NewGuid() },
                
                // Full role management
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111115")  // Roles.Create
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111116")  // Roles.Read
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111117")  // Roles.Update
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111118")  // Roles.Delete
                ) { Id = Guid.NewGuid() },
                
                // All messaging permissions
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111119")  // Messages.Send
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111a")  // Messages.Read
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111b")  // Messages.Edit
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111c")  // Messages.Delete
                ) { Id = Guid.NewGuid() },
                
                // All file permissions
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111d")  // Files.Upload
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111e")  // Files.Download
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-11111111111f")  // Files.Delete
                ) { Id = Guid.NewGuid() },
                
                // All group permissions
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111120")  // Groups.Create
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111121")  // Groups.Manage
                ) { Id = Guid.NewGuid() },
                new RolePermission(
                    Guid.Parse("22222222-2222-2222-2222-222222222223"),
                    Guid.Parse("11111111-1111-1111-1111-111111111122")  // Groups.Delete
                ) { Id = Guid.NewGuid() },
            };

            // Combine all role permissions
            var allRolePermissions = userRolePermissions
                .Concat(operatorRolePermissions)
                .Concat(adminRolePermissions);

            await context.RolePermissions.AddRangeAsync(allRolePermissions);
            logger.LogInformation("Seeded {Count} role-permission assignments", allRolePermissions.Count());
        }

        /// <summary>
        /// Creates the initial administrator user
        /// This is essential so you can log in and manage the system
        /// </summary>
        private static async Task SeedAdminUserAsync(IdentityDbContext context, IPasswordHasher passwordHasher, ILogger logger)
        {
            // Check if users already exist
            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Users already exist, skipping admin user seeding");
                return;
            }

            logger.LogInformation("Seeding admin user...");

            // Create the system administrator user
            // Password: Admin@123! (Remember to change this in production!)
            // The password hash was generated using BCrypt with work factor 12
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var adminUser = new User(
                username: "admin",
                email: "admin@chatapp.com",
                passwordHash: passwordHasher.Hash("Yusif2000+"),
                displayName: "System Administrator",
                createdBy: adminUserId, // Self-created
                avatarUrl: null,
                notes: "System administrator account created during initial setup",
                isAdmin: true
            )
            {
                Id = adminUserId
            };

            await context.Users.AddAsync(adminUser);

            // Assign the Administrator role to the admin user
            var adminUserRole = new UserRole(
                userId: adminUserId,
                roleId: Guid.Parse("11111111-1111-1111-1111-111111111111"), // Administrator role
                assignedBy: adminUserId // Self-assigned
            )
            {
                Id = Guid.NewGuid()
            };

            await context.UserRoles.AddAsync(adminUserRole);

            logger.LogInformation("Seeded admin user with username 'admin' and password 'Admin@123!'");
            logger.LogWarning("SECURITY: Remember to change the admin password after first login!");
        }
    }
}