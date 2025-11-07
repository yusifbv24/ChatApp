using ChatApp.Modules.Notifications.Application.DTOs;
using ChatApp.Modules.Notifications.Domain.Entities;
using ChatApp.Modules.Notifications.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Notifications.Infrastructure.Persistence
{
    public class NotificationsDbContext : DbContext
    {
        public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
        {
        }

        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new NotificationConfiguration());

            // Map Identity module's users table (read-only)
            modelBuilder.Entity<UserReadModel>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });
        }
    }

}