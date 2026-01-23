using ChatApp.Modules.DirectMessages.Application.DTOs.Request;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Configurations;
using ChatApp.Modules.Files.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence
{
    public class DirectMessagesDbContext : DbContext
    {
        public DirectMessagesDbContext(DbContextOptions<DirectMessagesDbContext> options) : base(options)
        {
        }

        public DbSet<DirectConversation> DirectConversations => Set<DirectConversation>();
        public DbSet<DirectConversationMember> DirectConversationMembers => Set<DirectConversationMember>();
        public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();
        public DbSet<DirectMessageReaction> DirectMessageReactions => Set<DirectMessageReaction>();
        public DbSet<DirectMessageMention> DirectMessageMentions => Set<DirectMessageMention>();
        public DbSet<UserFavoriteMessage> UserFavoriteMessages => Set<UserFavoriteMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply DirectMessages module configurations
            modelBuilder.ApplyConfiguration(new DirectConversationConfiguration());
            modelBuilder.ApplyConfiguration(new DirectConversationMemberConfiguration());
            modelBuilder.ApplyConfiguration(new DirectMessageConfiguration());
            modelBuilder.ApplyConfiguration(new DirectMessageReactionConfiguration());
            modelBuilder.ApplyConfiguration(new DirectMessageMentionConfiguration());
            modelBuilder.ApplyConfiguration(new UserFavoriteMessageConfiguration());

            // Map Identity module's users table (read-only for queries)
            modelBuilder.Entity<UserReadModel>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.FirstName).HasColumnName("first_name");
                entity.Property(e => e.LastName).HasColumnName("last_name");
                entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");

                // Ignore computed property
                entity.Ignore(e => e.FullName);

                // Mark as query-only (no tracking, no inserts/updates)
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });

            // Map Files module's file_metadata table (read-only for queries)
            modelBuilder.Entity<FileMetadata>(entity =>
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