using ChatApp.Modules.Identity.Domain.Repositories;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Modules.Identity.Infrastructure.Persistence;
using ChatApp.Modules.Identity.Infrastructure.Persistence.Repositories;
using ChatApp.Modules.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Modules.Identity.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services,IConfiguration configuration)
        {
            // Database
            var connectionString = configuration.GetConnectionString("IdentityDb");
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Services
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
            return services;
        }
    }
}