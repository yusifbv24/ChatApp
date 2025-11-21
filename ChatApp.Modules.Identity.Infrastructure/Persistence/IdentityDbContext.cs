using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence
{
    public class IdentityDbContext: DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options):base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles=>Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserRole> UserRoles=> Set<UserRole>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<RolePermission> RolePermissions=>Set<RolePermission>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new PermissionConfiguration());
            modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
            modelBuilder.ApplyConfiguration(new UserPermissionConfiguration());
            modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var permissions = new[]
            {
                new Permission("Users.Create", "Create users", "Identity") { Id = Guid.Parse("11111111-1111-1111-1111-111111111111") },
                new Permission("Users.Read", "Read users", "Identity") { Id = Guid.Parse("11111111-1111-1111-1111-111111111112") },
                new Permission("Users.Update", "Update users", "Identity") { Id = Guid.Parse("11111111-1111-1111-1111-111111111113") },
                new Permission("Users.Delete", "Delete users", "Identity") { Id = Guid.Parse("11111111-1111-1111-1111-111111111114") },
                new Permission("Roles.Create", "Create roles", "Identity") { Id = Guid.Parse("11111111-1111-1111-1111-111111111115") },
                new Permission("Roles.Read", "Read roles", "Identity") { Id = Guid.Parse("11111111-1111-1111-1111-111111111116") },
                new Permission("Roles.Update", "Update roles", "Identity") { Id = Guid.Parse("11111111-1111-1111-1111-111111111117") },
                new Permission("Roles.Delete", "Delete roles", "Identity") { Id = Guid.Parse("11111111-1111-1111-1111-111111111118") },
                new Permission("Messages.Send", "Send messages", "Messaging") { Id = Guid.Parse("11111111-1111-1111-1111-111111111119") },
                new Permission("Messages.Read", "Read messages", "Messaging") { Id = Guid.Parse("11111111-1111-1111-1111-11111111111a") },
                new Permission("Messages.Edit", "Edit messages", "Messaging") { Id = Guid.Parse("11111111-1111-1111-1111-11111111111b") },
                new Permission("Messages.Delete", "Delete messages", "Messaging") { Id = Guid.Parse("11111111-1111-1111-1111-11111111111c") },
                new Permission("Files.Upload", "Upload files", "Files") { Id = Guid.Parse("11111111-1111-1111-1111-11111111111d") },
                new Permission("Files.Download", "Download files", "Files") { Id = Guid.Parse("11111111-1111-1111-1111-11111111111e") },
                new Permission("Files.Delete", "Delete files", "Files") { Id = Guid.Parse("11111111-1111-1111-1111-11111111111f") },
                new Permission("Groups.Create", "Create groups", "Messaging") { Id = Guid.Parse("11111111-1111-1111-1111-111111111120") },
                new Permission("Groups.Manage", "Manage groups", "Messaging") { Id = Guid.Parse("11111111-1111-1111-1111-111111111121") },
            };

            modelBuilder.Entity<Permission>().HasData(permissions);

            // Seed Roles
            var userRole = new Role("User", "Basic user role", true) { Id = Guid.Parse("22222222-2222-2222-2222-222222222221") };
            var operatorRole = new Role("Operator", "Operator role with extended permissions", true) { Id = Guid.Parse("22222222-2222-2222-2222-222222222222") };

            modelBuilder.Entity<Role>().HasData(userRole, operatorRole);

            // Assign permissions to User role (basic permissions)
            var userRolePermissions = new[]
            {
            new RolePermission(userRole.Id, Guid.Parse("11111111-1111-1111-1111-111111111119")) { Id = Guid.NewGuid() }, // Messages.Send
            new RolePermission(userRole.Id, Guid.Parse("11111111-1111-1111-1111-11111111111a")) { Id = Guid.NewGuid() }, // Messages.Read
            new RolePermission(userRole.Id, Guid.Parse("11111111-1111-1111-1111-11111111111d")) { Id = Guid.NewGuid() }, // Files.Upload
            new RolePermission(userRole.Id, Guid.Parse("11111111-1111-1111-1111-11111111111e")) { Id = Guid.NewGuid() }, // Files.Download
            };

            modelBuilder.Entity<RolePermission>().HasData(userRolePermissions);
        }
    }
}