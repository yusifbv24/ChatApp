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
            modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());

            // Seed initial data (3 system roles with permissions)
            SeedData.Seed(modelBuilder);
        }
    }
}