using ChatApp.Modules.DirectMessages.Application.Behaviors;
using ChatApp.Modules.DirectMessages.Application.Commands.DirectConversations;
using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Infrastructure.Persistence;
using ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Modules.DirectMessages.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDirectMessagesInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database - Use same connection string as other modules
            var connectionString = configuration.GetConnectionString("IdentityDb")
                ?? throw new InvalidOperationException("Database connection string not configured");

            services.AddDbContext<DirectMessagesDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(DirectMessagesDbContext).Assembly.FullName);
                    // Configure split query behavior to avoid cartesian explosion warning
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                }));

            // Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IDirectConversationRepository, DirectConversationRepository>();
            services.AddScoped<IDirectMessageRepository, DirectMessageRepository>();

            return services;
        }

        public static IServiceCollection AddDirectMessagesApplication(
            this IServiceCollection services)
        {
            // Register MediatR for CQRS pattern
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
                typeof(StartConversationCommand).Assembly));

            // Register FluentValidation
            services.AddValidatorsFromAssembly(
                typeof(StartConversationCommand).Assembly);

            // Add validation behavior to MediatR pipeline
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }
    }
}