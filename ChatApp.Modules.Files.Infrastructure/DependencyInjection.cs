using ChatApp.Modules.Files.Application.Behaviors;
using ChatApp.Modules.Files.Application.Commands.UploadFile;
using ChatApp.Modules.Files.Application.Interfaces;
using ChatApp.Modules.Files.Infrastructure.Persistence;
using ChatApp.Modules.Files.Infrastructure.Persistence.Repositories;
using ChatApp.Modules.Files.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Modules.Files.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddFilesInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database
            var connectionString = configuration.GetConnectionString("IdentityDb")
                ?? throw new InvalidOperationException("Database connection string not configured");

            services.AddDbContext<FilesDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(FilesDbContext).Assembly.FullName);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                }));

            // Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IFileRepository, FileRepository>();

            // Storage Service
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
            services.AddScoped<IVirusScanningService, ClamAVScanningService>();

            return services;
        }

        public static IServiceCollection AddFilesApplication(
            this IServiceCollection services)
        {
            // Register MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
                typeof(UploadFileCommand).Assembly));

            // Register FluentValidation
            services.AddValidatorsFromAssembly(typeof(UploadFileCommand).Assembly);

            // Add validation behavior
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }
    }
}