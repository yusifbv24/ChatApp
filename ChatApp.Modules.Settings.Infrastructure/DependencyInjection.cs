using ChatApp.Modules.Notifications.Application.Behaviors;
using ChatApp.Modules.Settings.Application.Commands.UpdateDisplaySettings;
using ChatApp.Modules.Settings.Application.Interfaces;
using ChatApp.Modules.Settings.Infrastructure.Persistence;
using ChatApp.Modules.Settings.Infrastructure.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Modules.Settings.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSettingsInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("IdentityDb")
                ?? throw new InvalidOperationException("Database connection string not configured");

            services.AddDbContext<SettingsDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(SettingsDbContext).Assembly.FullName);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                }));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

            return services;
        }

        public static IServiceCollection AddSettingsApplication(
            this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
                typeof(UpdateDisplaySettingsCommand).Assembly));

            services.AddValidatorsFromAssembly(typeof(UpdateDisplaySettingsCommand).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }
    }
}