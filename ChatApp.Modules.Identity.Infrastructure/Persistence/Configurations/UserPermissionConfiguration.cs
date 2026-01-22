using ChatApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations
{
    public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
    {
        public void Configure(EntityTypeBuilder<UserPermission> builder)
        {
            builder.ToTable("user_permissions");

            builder.HasKey(up => up.Id);

            builder.Property(up => up.Id)
                .HasColumnName("id");

            builder.Property(up => up.UserId)
                .IsRequired()
                .HasColumnName("user_id");

            builder.Property(up => up.PermissionName)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("permission_name");

            builder.Property(up => up.CreatedAtUtc)
                .IsRequired()
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone");

            builder.Property(up => up.UpdatedAtUtc)
                .IsRequired()
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone");

            // Composite unique index (User can have each permission only once)
            builder.HasIndex(up => new { up.UserId, up.PermissionName })
                .IsUnique()
                .HasDatabaseName("ix_user_permissions_user_id_permission_name");

            // Index on permission name for lookup performance
            builder.HasIndex(up => up.PermissionName)
                .HasDatabaseName("ix_user_permissions_permission_name");

            // Relationships
            builder.HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}