using ChatApp.Modules.DirectMessages.Application.DTOs.Request;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence
{
    public class DirectMessagesDbContext : DbContext
    {
        public DirectMessagesDbContext(DbContextOptions<DirectMessagesDbContext> options) : base(options)
        {
        }

        public DbSet<DirectConversation> DirectConversations => Set<DirectConversation>();
        public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();
        public DbSet<DirectMessageReaction> DirectMessageReactions => Set<DirectMessageReaction>();
        public DbSet<UserFavoriteMessage> UserFavoriteMessages => Set<UserFavoriteMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply DirectMessages module configurations
            modelBuilder.ApplyConfiguration(new DirectConversationConfiguration());
            modelBuilder.ApplyConfiguration(new DirectMessageConfiguration());
            modelBuilder.ApplyConfiguration(new DirectMessageReactionConfiguration());
            modelBuilder.ApplyConfiguration(new UserFavoriteMessageConfiguration());

            // Map Identity module's users table (read-only for queries)
            modelBuilder.Entity<UserReadModel>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");

                // Mark as query-only (no tracking, no inserts/updates)
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });

            // Map Files module's file_metadata table (read-only for queries)
            modelBuilder.Entity<ChatApp.Modules.Files.Domain.Entities.FileMetadata>(entity =>
            {
                entity.ToTable("file_metadata");
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