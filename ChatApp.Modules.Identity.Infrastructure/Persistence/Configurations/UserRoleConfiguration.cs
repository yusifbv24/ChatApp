using ChatApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("user_roles");

            builder.HasKey(ur => ur.Id);

            builder.Property(ur => ur.Id)
                .HasColumnName("id");

            builder.Property(ur => ur.Id)
                .HasColumnName("id");

            builder.Property(ur => ur.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(ur => ur.RoleId)
                .HasColumnName("role_id")
                .IsRequired();

            builder.Property(ur => ur.AssignedAtUtc)
                .HasColumnName("assigned_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(ur => ur.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(ur => ur.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
        }
    }
}