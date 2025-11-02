using ChatApp.Modules.Channels.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Configurations
{
    public class ChannelMessageReadConfiguration : IEntityTypeConfiguration<ChannelMessageRead>
    {
        public void Configure(EntityTypeBuilder<ChannelMessageRead> builder)
        {
            builder.ToTable("channel_message_reads");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .HasColumnName("id");

            builder.Property(r => r.MessageId)
                .HasColumnName("message_id")
                .IsRequired();

            builder.Property(r => r.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(r => r.ReadAtUtc)
                .HasColumnName("read_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(r => r.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(r => r.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Indexes
            builder.HasIndex(r => new { r.MessageId, r.UserId })
                .IsUnique()
                .HasDatabaseName("ix_channel_message_reads_message_user");

            builder.HasIndex(r => r.UserId)
                .HasDatabaseName("ix_channel_message_reads_user_id");
        }
    }
}