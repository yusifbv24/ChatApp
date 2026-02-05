using ChatApp.Modules.DirectMessages.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Configurations
{
    public class DirectMessageMentionConfiguration : IEntityTypeConfiguration<DirectMessageMention>
    {
        public void Configure(EntityTypeBuilder<DirectMessageMention> builder)
        {
            builder.ToTable("direct_message_mentions");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                .HasColumnName("id");

            builder.Property(m => m.MessageId)
                .HasColumnName("message_id")
                .IsRequired();

            builder.Property(m => m.MentionedUserId)
                .HasColumnName("mentioned_user_id")
                .IsRequired();

            builder.Property(m => m.MentionedUserFullName)
                .HasColumnName("mentioned_user_name") // Keep DB column name for backward compatibility
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(m => m.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(m => m.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Relationships
            builder.HasOne(m => m.Message)
                .WithMany(msg => msg.Mentions)
                .HasForeignKey(m => m.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(m => m.MessageId)
                .HasDatabaseName("ix_direct_message_mentions_message_id");

            builder.HasIndex(m => m.MentionedUserId)
                .HasDatabaseName("ix_direct_message_mentions_mentioned_user_id");
        }
    }
}