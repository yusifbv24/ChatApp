using ChatApp.Modules.Search.Application.DTOs.Responses;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Search.Infrastructure.Persistence
{
    public class SearchDbContext(DbContextOptions<SearchDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map read-only entities from other modules


            modelBuilder.Entity<UserReadModel>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });

            modelBuilder.Entity<ChannelReadModel>(entity =>
            {
                entity.ToTable("channels");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });

            modelBuilder.Entity<ChannelMessageReadModel>(entity =>
            {
                entity.ToTable("channel_messages");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ChannelId).HasColumnName("channel_id");
                entity.Property(e => e.SenderId).HasColumnName("sender_id");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
                entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });

            modelBuilder.Entity<ChannelMemberReadModel>(entity =>
            {
                entity.ToTable("channel_members");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ChannelId).HasColumnName("channel_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.LeftAtUtc).HasColumnName("left_at_utc");
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });

            modelBuilder.Entity<DirectMessageReadModel>(entity =>
            {
                entity.ToTable("direct_messages");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.SenderId).HasColumnName("sender_id");
                entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
                entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });

            modelBuilder.Entity<ConversationReadModel>(entity =>
            {
                entity.ToTable("direct_conversations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.User1Id).HasColumnName("user1_id");
                entity.Property(e => e.User2Id).HasColumnName("user2_id");
                entity.ToTable(tb => tb.ExcludeFromMigrations());
            });
        }
    }
}