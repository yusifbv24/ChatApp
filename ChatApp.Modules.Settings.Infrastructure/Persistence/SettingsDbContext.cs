using ChatApp.Modules.Settings.Domain.Entities;
using ChatApp.Modules.Settings.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Settings.Infrastructure.Persistence
{
    public class SettingsDbContext : DbContext
    {
        public SettingsDbContext(DbContextOptions<SettingsDbContext> options) : base(options)
        {
        }

        public DbSet<UserSettings> UserSettings => Set<UserSettings>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserSettingsConfiguration());
        }
    }
}