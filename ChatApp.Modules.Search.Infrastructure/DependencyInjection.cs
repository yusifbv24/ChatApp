using ChatApp.Modules.Search.Application.Behaviors;
using ChatApp.Modules.Search.Application.Interfaces;
using ChatApp.Modules.Search.Application.Queries.SearchMessages;
using ChatApp.Modules.Search.Infrastructure.Persistence;
using ChatApp.Modules.Search.Infrastructure.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Modules.Search.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSearchInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database (read-only acccess to other modules' tables)
            var connectionString = configuration.GetConnectionString("IdentityDb")
                ?? throw new InvalidOperationException("Database connection string not configured");

            services.AddDbContext<SearchDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(SearchDbContext).Assembly.FullName);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                }));

            services.AddScoped<ISearchRepository, SearchRepository>();
            return services;
        }

        public static IServiceCollection AddSearchApplication(
            this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
                typeof(SearchMessagesQuery).Assembly));

            services.AddValidatorsFromAssembly(typeof(SearchMessagesQuery).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }
    }
}