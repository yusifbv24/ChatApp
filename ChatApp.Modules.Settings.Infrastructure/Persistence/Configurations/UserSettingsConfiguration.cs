using ChatApp.Modules.Settings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Settings.Infrastructure.Persistence.Configurations
{
    public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
    {
        public void Configure(EntityTypeBuilder<UserSettings> builder)
        {
            builder.ToTable("user_settings");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .HasColumnName("id");

            builder.Property(s => s.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            // Notification Settings
            builder.Property(s => s.EmailNotificationsEnabled)
                .HasColumnName("email_notifications_enabled")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.PushNotificationsEnabled)
                .HasColumnName("push_notifications_enabled")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.NotifyOnChannelMessage)
                .HasColumnName("notify_on_channel_message")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.NotifyOnDirectMessage)
                .HasColumnName("notify_on_direct_message")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.NotifyOnMention)
                .HasColumnName("notify_on_mention")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.NotifyOnReaction)
                .HasColumnName("notify_on_reaction")
                .IsRequired()
                .HasDefaultValue(true);

            // Privacy Settings
            builder.Property(s => s.ShowOnlineStatus)
                .HasColumnName("show_online_status")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.ShowLastSeen)
                .HasColumnName("show_last_seen")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.ShowReadReceipts)
                .HasColumnName("show_read_receipts")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.AllowDirectMessages)
                .HasColumnName("allow_direct_messages")
                .IsRequired()
                .HasDefaultValue(true);

            // Display Settings
            builder.Property(s => s.Theme)
                .HasColumnName("theme")
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("light");

            builder.Property(s => s.Language)
                .HasColumnName("language")
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("en");

            builder.Property(s => s.MessagePageSize)
                .HasColumnName("message_page_size")
                .IsRequired()
                .HasDefaultValue(50);

            builder.Property(s => s.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(s => s.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Indexes
            builder.HasIndex(s => s.UserId)
                .IsUnique()
                .HasDatabaseName("ix_user_settings_user_id");
        }
    }
}