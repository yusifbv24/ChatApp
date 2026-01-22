using ChatApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.ToTable("departments");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id)
                .HasColumnName("id");

            builder.Property(d => d.Name)
                .IsRequired()
                .HasColumnName("name")
                .HasMaxLength(200);

            builder.Property(d => d.ParentDepartmentId)
                .HasColumnName("parent_department_id");

            builder.Property(d => d.HeadOfDepartmentId)
                .HasColumnName("head_of_department_id");

            builder.Property(d => d.CreatedAtUtc)
                .IsRequired()
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone");

            builder.Property(d => d.UpdatedAtUtc)
                .IsRequired()
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone");

            // Indexes
            builder.HasIndex(d => d.Name)
                .HasDatabaseName("ix_departments_name");

            builder.HasIndex(d => d.ParentDepartmentId)
                .HasDatabaseName("ix_departments_parent_department_id");

            builder.HasIndex(d => d.HeadOfDepartmentId)
                .HasDatabaseName("ix_departments_head_of_department_id");

            // Relationships

            // Self-referencing (Parent Department -> Subdepartments)
            builder.HasOne(d => d.ParentDepartment)
                .WithMany(d => d.Subdepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Head of Department (already configured in UserConfiguration)
            // Employees relationship (already configured in UserConfiguration)
            // Positions relationship (already configured in PositionConfiguration)
        }
    }
}