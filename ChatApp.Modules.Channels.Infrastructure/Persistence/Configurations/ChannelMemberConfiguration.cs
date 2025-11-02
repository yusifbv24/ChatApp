using ChatApp.Modules.Channels.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Configurations
{
    public class ChannelMemberConfiguration : IEntityTypeConfiguration<ChannelMember>
    {
        public void Configure(EntityTypeBuilder<ChannelMember> builder)
        {
            builder.ToTable("channel_members");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                .HasColumnName("id");

            builder.Property(m => m.ChannelId)
                .HasColumnName("channel_id")
                .IsRequired();

            builder.Property(m => m.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(m => m.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(m => m.JoinedAtUtc)
                .HasColumnName("joined_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(m => m.LeftAtUtc)
                .HasColumnName("left_at_utc")
                .HasColumnType("timestamp with time zone");

            builder.Property(m => m.IsActive)
                .HasColumnName("is_active")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(m => m.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(m => m.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Indexes
            builder.HasIndex(m => new { m.ChannelId, m.UserId })
                .IsUnique()
                .HasDatabaseName("ix_channel_members_channel_user");

            builder.HasIndex(m => m.UserId)
                .HasDatabaseName("ix_channel_members_user_id");

            builder.HasIndex(m => m.IsActive)
                .HasDatabaseName("ix_channel_members_is_active");
        }
    }
}