using ChatApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations
{
    public class PositionConfiguration : IEntityTypeConfiguration<Position>
    {
        public void Configure(EntityTypeBuilder<Position> builder)
        {
            builder.ToTable("positions");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .HasColumnName("id");

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnName("name");

            builder.Property(p => p.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            builder.Property(p => p.DepartmentId)
                .HasColumnName("department_id");

            builder.Property(p => p.CreatedAtUtc)
                .IsRequired()
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.UpdatedAtUtc)
                .IsRequired()
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone");

            // Indexes
            builder.HasIndex(p => p.DepartmentId)
                .HasDatabaseName("ix_positions_department_id");

            // Composite index for department + name (same position name can exist in different departments)
            builder.HasIndex(p => new { p.DepartmentId, p.Name })
                .HasDatabaseName("ix_positions_department_id_name");

            // Relationships
            builder.HasOne(p => p.Department)
                .WithMany(d => d.Positions)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Users collection is configured in UserConfiguration
        }
    }
}
