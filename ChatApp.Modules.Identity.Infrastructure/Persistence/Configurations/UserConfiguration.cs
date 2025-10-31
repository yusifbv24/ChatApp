using ChatApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration:IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasColumnType("id");

            builder.Property(u => u.Username)
                .IsRequired()
                .HasColumnName("username")
                .HasMaxLength(50);

            builder.Property(u=> u.Email)
                .IsRequired()
                .HasColumnName("email")
                .HasMaxLength(255);

            builder.Property(u=>u.PasswordHash)
                .IsRequired()
                .HasColumnName("password")
                .HasMaxLength(255);

            builder.Property(u => u.DisplayName)
                .IsRequired()
                .HasColumnName("display_name")
                .HasMaxLength(100);

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            builder.Property(u => u.IsAdmin)
                .IsRequired()
                .HasColumnName("is_admin")
                .HasDefaultValue(false);

            builder.Property(u => u.AvatarUrl)
                .HasColumnName("avatar_url")
                .HasMaxLength(500);

            builder.Property(u=>u.Notes)
                .HasColumnName("notes")
                .HasMaxLength(1000);

            builder.Property(u => u.CreatedBy)
                .HasColumnName("created_by");

            builder.Property(u => u.CreatedAtUtc)
                .IsRequired()
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone");

            builder.Property(u => u.UpdatedAtUtc)
                .IsRequired()
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone");

            builder.HasIndex(u => u.Id)
                .IsUnique()
                .HasDatabaseName("ix_users_id");

            builder.HasIndex(u => u.DisplayName)
                .IsUnique()
                .HasDatabaseName("ix_users_display_name");

            builder.HasIndex(u => u.Email)
                .IsUnique();

            // Relationships
            builder.HasMany(u=>u.UserRoles)
                .WithOne(ur=>ur.User)
                .HasForeignKey(ur=>ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}