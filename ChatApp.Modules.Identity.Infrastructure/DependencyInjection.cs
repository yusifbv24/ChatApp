using ChatApp.Modules.Identity.Application.Behaviors;
using ChatApp.Modules.Identity.Application.Commands.Users;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Modules.Identity.Infrastructure.Persistence;
using ChatApp.Modules.Identity.Infrastructure.Persistence.Repositories;
using ChatApp.Modules.Identity.Infrastructure.Services;
using FluentValidation;
using MediatR;
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
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                }));

            // Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Services
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
            services.AddSingleton<IEncryptionService, AesEncryptionService>();

            return services;
        }

        public static IServiceCollection AddIdentityApplication(
            this IServiceCollection services)
        {
            // Register MediatR for CQRS pattern
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
                typeof(CreateUserCommand).Assembly));

            // Register FluentValidation
            services.AddValidatorsFromAssembly(
                typeof(CreateUserCommand).Assembly);

            // Add validation behavior to MediatR pipeline
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }
    }
}