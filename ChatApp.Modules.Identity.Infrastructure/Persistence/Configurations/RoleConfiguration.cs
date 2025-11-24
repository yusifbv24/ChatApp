using ChatApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .HasColumnName("id");

            builder.Property(r => r.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(r => r.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            builder.Property(r => r.IsSystemRole)
                .HasColumnName("is_system_role")
                .IsRequired();

            builder.Property(r => r.SystemRoleType)
                .HasColumnName("system_role_type")
                .IsRequired(false);

            builder.Property(r => r.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(r => r.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasIndex(r => r.Name).IsUnique();

            // Relationships
            builder.HasMany(r => r.RolePermissions)
                .WithOne(rp => rp.Role)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.UserRoles)
                .WithOne(ur => ur.Role)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}