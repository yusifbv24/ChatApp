using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatApp.Modules.Channels.Domain.Entities;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Configurations
{
    public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
    {
        public void Configure(EntityTypeBuilder<Channel> builder)
        {
            builder.ToTable("channels");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasColumnName("id");

            builder.Property(c => c.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            builder.Property(c => c.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(c => c.CreatedBy)
                .HasColumnName("created_by")
                .IsRequired();

            builder.Property(c => c.IsArchived)
                .HasColumnName("is_archived")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(c => c.ArchivedAtUtc)
                .HasColumnName("archived_at_utc")
                .HasColumnType("timestamp with time zone");

            builder.Property(c => c.AvatarUrl)
                .HasColumnName("avatar_url")
                .HasMaxLength(500);

            builder.Property(c => c.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(c => c.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Indexes
            builder.HasIndex(c => c.Name)
                .IsUnique()
                .HasDatabaseName("ix_channels_name");

            builder.HasIndex(c => c.Type)
                .HasDatabaseName("ix_channels_type");

            builder.HasIndex(c => c.CreatedBy)
                .HasDatabaseName("ix_channels_created_by");

            builder.HasIndex(c => c.IsArchived)
                .HasDatabaseName("ix_channels_is_archived");

            // Relationships
            builder.HasMany(c => c.Members)
                .WithOne(m => m.Channel)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Messages)
                .WithOne(m => m.Channel)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}