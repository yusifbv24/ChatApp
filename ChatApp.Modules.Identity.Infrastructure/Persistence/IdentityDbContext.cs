using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence
{
    public class IdentityDbContext: DbContext
    {
        private readonly IEncryptionService? _encryptionService;

        public IdentityDbContext(
            DbContextOptions<IdentityDbContext> options,
            IServiceProvider serviceProvider) : base(options)
        {
            // Get encryption service from DI (will be null during migrations)
            _encryptionService = serviceProvider.GetService<IEncryptionService>();
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NormalizeDateTimesToUtc();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Ensures all DateTime values have Kind=UTC before saving to PostgreSQL.
        /// PostgreSQL 'timestamp with time zone' columns reject Kind=Unspecified.
        /// </summary>
        private void NormalizeDateTimesToUtc()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                    continue;

                foreach (var property in entry.Properties)
                {
                    if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Unspecified)
                    {
                        property.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyConfiguration());
            modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
            modelBuilder.ApplyConfiguration(new PositionConfiguration());
            modelBuilder.ApplyConfiguration(new UserPermissionConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());

            // Apply EmployeeConfiguration with encryption service
            if (_encryptionService != null)
            {
                modelBuilder.ApplyConfiguration(new EmployeeConfiguration(_encryptionService));
            }
        }
    }
}