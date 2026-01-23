using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Modules.Identity.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for Employee entity
    /// Configures encryption for sensitive fields (DateOfBirth, WorkPhone, AboutMe)
    /// </summary>
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        private readonly IEncryptionService _encryptionService;

        public EmployeeConfiguration(IEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("employees");

            // Primary Key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            // Foreign Key to User (1:1 mandatory relationship)
            builder.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.HasOne(e => e.User)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint on UserId (ensures 1:1 relationship)
            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_employees_user_id")
                .IsUnique();

            // Sensitive Personal Information (ENCRYPTED)
            builder.Property(e => e.DateOfBirth)
                .HasColumnName("date_of_birth")
                .HasColumnType("text") // Stored as encrypted string
                .HasConversion(new EncryptedDateTimeConverter(_encryptionService));

            builder.Property(e => e.WorkPhone)
                .HasColumnName("work_phone")
                .HasColumnType("text")
                .HasMaxLength(500) // Encrypted data is longer than plaintext
                .HasConversion(new EncryptedStringConverter(_encryptionService));

            builder.Property(e => e.AboutMe)
                .HasColumnName("about_me")
                .HasColumnType("text")
                .HasConversion(new EncryptedStringConverter(_encryptionService));

            // Organizational Structure
            builder.Property(e => e.PositionId)
                .HasColumnName("position_id");

            builder.HasOne(e => e.Position)
                .WithMany(p => p.Employees)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(e => e.DepartmentId)
                .HasColumnName("department_id");

            builder.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(e => e.SupervisorId)
                .HasColumnName("supervisor_id");

            builder.HasOne(e => e.Supervisor)
                .WithMany(e => e.Subordinates)
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(e => e.HeadOfDepartmentId)
                .HasColumnName("head_of_department_id");

            // Employment Information
            builder.Property(e => e.HiringDate)
                .HasColumnName("hiring_date")
                .HasColumnType("timestamp with time zone");

            // Timestamps
            builder.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(e => e.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Indexes for performance
            builder.HasIndex(e => e.DepartmentId)
                .HasDatabaseName("ix_employees_department_id");

            builder.HasIndex(e => e.PositionId)
                .HasDatabaseName("ix_employees_position_id");

            builder.HasIndex(e => e.SupervisorId)
                .HasDatabaseName("ix_employees_supervisor_id");
        }
    }
}