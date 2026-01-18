using ChatApp.Modules.DirectMessages.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Configurations
{
    public class DirectConversationConfiguration:IEntityTypeConfiguration<DirectConversation>
    {
        public void Configure(EntityTypeBuilder<DirectConversation> builder)
        {
            builder.ToTable("direct_conversations");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasColumnName("id");

            builder.Property(c => c.User1Id)
                .HasColumnName("user1_id")
                .IsRequired();

            builder.Property(c => c.User2Id)
                .HasColumnName("user2_id")
                .IsRequired();

            builder.Property(c => c.LastMessageAtUtc)
                .HasColumnName("last_message_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(c => c.IsUser1Active)
                .HasColumnName("is_user1_active")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(c => c.IsUser2Active)
                .HasColumnName("is_user2_active")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(c => c.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(c => c.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(c => c.User1IsPinned)
                .HasColumnName("user1_is_pinned")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(c => c.User2IsPinned)
                .HasColumnName("user2_is_pinned")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(c => c.User1IsMuted)
                .HasColumnName("user1_is_muted")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(c => c.User2IsMuted)
                .HasColumnName("user2_is_muted")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(c => c.User1IsMarkedReadLater)
                .HasColumnName("user1_is_marked_read_later")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(c => c.User2IsMarkedReadLater)
                .HasColumnName("user2_is_marked_read_later")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(c => c.User1LastReadLaterMessageId)
                .HasColumnName("user1_last_read_later_message_id");

            builder.Property(c => c.User2LastReadLaterMessageId)
                .HasColumnName("user2_last_read_later_message_id");

            builder.Property(c => c.InitiatedByUserId)
                .HasColumnName("initiated_by_user_id")
                .IsRequired();

            builder.Property(c => c.HasMessages)
                .HasColumnName("has_messages")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(c => c.IsNotes)
                .HasColumnName("is_notes")
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes for performance
            builder.HasIndex(c => new { c.User1Id, c.User2Id })
                .IsUnique()
                .HasDatabaseName("ix_direct_conversations_users");

            builder.HasIndex(c => c.User1Id)
                .HasDatabaseName("ix_direct_conversations_user1_id");

            builder.HasIndex(c => c.User2Id)
                .HasDatabaseName("ix_direct_conversations_user2_id");

            builder.HasIndex(c => c.LastMessageAtUtc)
                .HasDatabaseName("ix_direct_conversations_last_message");

            // Relationships
            builder.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}