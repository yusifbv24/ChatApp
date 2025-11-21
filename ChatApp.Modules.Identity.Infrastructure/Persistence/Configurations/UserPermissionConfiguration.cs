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
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(up => up.PermissionId)
                .HasColumnName("permission_id")
                .IsRequired();

            builder.Property(up => up.IsGranted)
                .HasColumnName("is_granted")
                .IsRequired();

            builder.Property(up => up.AssignedAtUtc)
                .HasColumnName("assigned_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(up => up.AssignedBy)
                .HasColumnName("assigned_by");

            builder.Property(up => up.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(up => up.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasIndex(up => new { up.UserId, up.PermissionId }).IsUnique();
        }
    }
}