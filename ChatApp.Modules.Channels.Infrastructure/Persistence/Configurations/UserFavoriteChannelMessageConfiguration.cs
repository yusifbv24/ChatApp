using ChatApp.Modules.Channels.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Configurations
{
    public class UserFavoriteChannelMessageConfiguration : IEntityTypeConfiguration<UserFavoriteChannelMessage>
    {
        public void Configure(EntityTypeBuilder<UserFavoriteChannelMessage> builder)
        {
            builder.ToTable("user_favorite_channel_messages");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Id)
                .HasColumnName("id");

            builder.Property(f => f.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(f => f.MessageId)
                .HasColumnName("message_id")
                .IsRequired();

            builder.Property(f => f.FavoritedAtUtc)
                .HasColumnName("favorited_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(f => f.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(f => f.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Unique constraint: one user can favorite a message only once
            builder.HasIndex(f => new { f.UserId, f.MessageId })
                .IsUnique()
                .HasDatabaseName("ix_user_favorite_channel_messages_unique");

            builder.HasIndex(f => f.UserId)
                .HasDatabaseName("ix_user_favorite_channel_messages_user_id");

            builder.HasIndex(f => f.MessageId)
                .HasDatabaseName("ix_user_favorite_channel_messages_message_id");
        }
    }
}
