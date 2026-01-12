using ChatApp.Modules.Channels.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Configurations
{
    public class ChannelMessageMentionConfiguration : IEntityTypeConfiguration<ChannelMessageMention>
    {
        public void Configure(EntityTypeBuilder<ChannelMessageMention> builder)
        {
            builder.ToTable("channel_message_mentions");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                .HasColumnName("id");

            builder.Property(m => m.MessageId)
                .HasColumnName("message_id")
                .IsRequired();

            builder.Property(m => m.MentionedUserId)
                .HasColumnName("mentioned_user_id");

            builder.Property(m => m.MentionedUserName)
                .HasColumnName("mentioned_user_name")
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(m => m.IsAllMention)
                .HasColumnName("is_all_mention")
                .IsRequired();

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
                .HasDatabaseName("ix_channel_message_mentions_message_id");

            builder.HasIndex(m => m.MentionedUserId)
                .HasDatabaseName("ix_channel_message_mentions_mentioned_user_id");
        }
    }
}