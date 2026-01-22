using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasColumnName("id");

            // Required fields
            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasColumnName("first_name")
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasColumnName("last_name")
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasColumnName("email")
                .HasMaxLength(255);

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasColumnName("password_hash")
                .HasMaxLength(255);

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            builder.Property(u => u.Role)
                .IsRequired()
                .HasColumnName("role")
                .HasConversion<int>() // Store enum as integer
                .HasDefaultValue(Role.User);

            // Optional fields
            builder.Property(u => u.DateOfBirth)
                .HasColumnName("date_of_birth")
                .HasColumnType("timestamp with time zone");

            builder.Property(u => u.AvatarUrl)
                .HasColumnName("avatar_url")
                .HasMaxLength(500);

            builder.Property(u => u.WorkPhone)
                .HasColumnName("work_phone")
                .HasMaxLength(50);

            builder.Property(u => u.HiringDate)
                .HasColumnName("hiring_date")
                .HasColumnType("timestamp with time zone");

            builder.Property(u => u.LastVisit)
                .HasColumnName("last_visit")
                .HasColumnType("timestamp with time zone");

            builder.Property(u => u.AboutMe)
                .HasColumnName("about_me")
                .HasMaxLength(2000);

            // Organizational structure
            builder.Property(u => u.PositionId)
                .HasColumnName("position_id");

            builder.Property(u => u.DepartmentId)
                .HasColumnName("department_id");

            builder.Property(u => u.SupervisorId)
                .HasColumnName("supervisor_id");

            builder.Property(u => u.HeadOfDepartmentId)
                .HasColumnName("head_of_department_id");

            // Timestamps
            builder.Property(u => u.CreatedAtUtc)
                .IsRequired()
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone");

            builder.Property(u => u.UpdatedAtUtc)
                .IsRequired()
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone");

            // Computed properties (not stored in database)
            builder.Ignore(u => u.FullName);
            builder.Ignore(u => u.IsAdmin);
            builder.Ignore(u => u.IsCEO);

            // Indexes
            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("ix_users_email");

            builder.HasIndex(u => u.PositionId)
                .HasDatabaseName("ix_users_position_id");

            builder.HasIndex(u => u.DepartmentId)
                .HasDatabaseName("ix_users_department_id");

            builder.HasIndex(u => u.SupervisorId)
                .HasDatabaseName("ix_users_supervisor_id");

            // Relationships

            // Position
            builder.HasOne(u => u.Position)
                .WithMany(p => p.Users)
                .HasForeignKey(u => u.PositionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Department
            builder.HasOne(u => u.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Supervisor (self-referencing)
            builder.HasOne(u => u.Supervisor)
                .WithMany(u => u.Subordinates)
                .HasForeignKey(u => u.SupervisorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Managed Departments (User as Head of Department)
            builder.HasMany(u => u.ManagedDepartments)
                .WithOne(d => d.HeadOfDepartment)
                .HasForeignKey(d => d.HeadOfDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // User Permissions
            builder.HasMany(u => u.UserPermissions)
                .WithOne(up => up.User)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
