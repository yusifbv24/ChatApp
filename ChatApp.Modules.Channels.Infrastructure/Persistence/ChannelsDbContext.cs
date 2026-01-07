using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Domain.Entities;
using ChatApp.Modules.Channels.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence
{
    public class ChannelsDbContext : DbContext
    {
        public ChannelsDbContext(DbContextOptions<ChannelsDbContext> options) : base(options)
        {
        }

        // Channels tables
        public DbSet<Channel> Channels => Set<Channel>();
        public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();
        public DbSet<ChannelMessage> ChannelMessages => Set<ChannelMessage>();
        public DbSet<ChannelMessageReaction> ChannelMessageReactions => Set<ChannelMessageReaction>();
        public DbSet<ChannelMessageRead> ChannelMessageReads => Set<ChannelMessageRead>();
        public DbSet<UserFavoriteChannelMessage> UserFavoriteChannelMessages => Set<UserFavoriteChannelMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply Channels module configurations
            modelBuilder.ApplyConfiguration(new ChannelConfiguration());
            modelBuilder.ApplyConfiguration(new ChannelMemberConfiguration());
            modelBuilder.ApplyConfiguration(new ChannelMessageConfiguration());
            modelBuilder.ApplyConfiguration(new ChannelMessageReactionConfiguration());
            modelBuilder.ApplyConfiguration(new ChannelMessageReadConfiguration());
            modelBuilder.ApplyConfiguration(new UserFavoriteChannelMessageConfiguration());

            // Map Identity module's users table (read-only for queries)
            // This allows us to join with users without creating dependency on Identity module
            modelBuilder.Entity<UserReadModel>(entity =>
            {
                entity.ToTable("users"); // Identity module's table
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");

                // Mark as query-only (no tracking, no inserts/updates)
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });

            // Map Files module's file_metadata table (read-only for queries)
            // This allows us to join with files without creating dependency on Files module
            modelBuilder.Entity<ChatApp.Modules.Files.Domain.Entities.FileMetadata>(entity =>
            {
                entity.ToTable("file_metadata"); // Files module's table
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.OriginalFileName).HasColumnName("original_file_name");
                entity.Property(e => e.ContentType).HasColumnName("content_type");
                entity.Property(e => e.FileSizeInBytes).HasColumnName("file_size_in_bytes");

                // Mark as query-only (no tracking, no inserts/updates)
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });
        }
    }
}