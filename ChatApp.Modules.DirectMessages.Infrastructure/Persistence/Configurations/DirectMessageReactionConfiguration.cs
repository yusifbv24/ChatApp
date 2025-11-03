using ChatApp.Modules.DirectMessages.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Configurations
{
    public class DirectMessageReactionConfiguration : IEntityTypeConfiguration<DirectMessageReaction>
    {
        public void Configure(EntityTypeBuilder<DirectMessageReaction> builder)
        {
            builder.ToTable("direct_message_reactions");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .HasColumnName("id");

            builder.Property(r => r.MessageId)
                .HasColumnName("message_id")
                .IsRequired();

            builder.Property(r => r.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(r => r.Reaction)
                .HasColumnName("reaction")
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(r => r.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(r => r.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Indexes
            builder.HasIndex(r => new { r.MessageId, r.UserId, r.Reaction })
                .IsUnique()
                .HasDatabaseName("ix_direct_message_reactions_unique");

            builder.HasIndex(r => r.MessageId)
                .HasDatabaseName("ix_direct_message_reactions_message_id");
        }
    }
}