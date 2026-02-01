using ChatApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("companies");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasColumnName("id");

            builder.Property(c => c.Name)
                .IsRequired()
                .HasColumnName("name")
                .HasMaxLength(200);

            builder.Property(c => c.HeadOfCompanyId)
                .HasColumnName("head_of_company_id");

            builder.Property(c => c.CreatedAtUtc)
                .IsRequired()
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone");

            builder.Property(c => c.UpdatedAtUtc)
                .IsRequired()
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone");

            // Indexes
            builder.HasIndex(c => c.Name)
                .HasDatabaseName("ix_companies_name");

            builder.HasIndex(c => c.HeadOfCompanyId)
                .HasDatabaseName("ix_companies_head_of_company_id");

            // Relationships
            builder.HasMany(c => c.Departments)
                .WithOne(d => d.Company)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
