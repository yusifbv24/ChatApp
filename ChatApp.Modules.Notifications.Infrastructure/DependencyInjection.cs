using ChatApp.Modules.Notifications.Application.Behaviors;
using ChatApp.Modules.Notifications.Application.Commands.SendNotification;
using ChatApp.Modules.Notifications.Application.Interfaces;
using ChatApp.Modules.Notifications.Infrastructure.BackgroundServices;
using ChatApp.Modules.Notifications.Infrastructure.Persistence;
using ChatApp.Modules.Notifications.Infrastructure.Repositories;
using ChatApp.Modules.Notifications.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Modules.Notifications.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddNotificationsInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("IdentityDb")
                ?? throw new InvalidOperationException("Database connection string not configured");

            services.AddDbContext<NotificationsDbContext>(options =>
                options.UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(NotificationsDbContext).Assembly.FullName)));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IEmailService, SmtpEmailService>();

            services.AddHostedService<EmailNotificationWorker>();

            return services;
        }

        public static IServiceCollection AddNotificationsApplication(
            this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
                typeof(SendNotificationCommand).Assembly));

            services.AddValidatorsFromAssembly(typeof(SendNotificationCommand).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }
    }
}