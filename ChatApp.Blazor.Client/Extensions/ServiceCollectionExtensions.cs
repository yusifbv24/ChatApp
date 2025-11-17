using ChatApp.Blazor.Client.Features.Admin.Services;
using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.State;

namespace ChatApp.Blazor.Client.Extensions
{
    /// <summary>
    /// Extension methods for service registration
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFeatureServices(this IServiceCollection services)
        {
            // Auth services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();

            // Admin services
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPermissionService, PermissionService>();

            return services;
        }

        /// <summary>
        /// Registers state management services
        /// </summary>
        public static IServiceCollection AddStateManagementServices(this IServiceCollection services)
        {
            services.AddScoped<AppState>();
            services.AddScoped<UserState>();
            return services;
        }
    }
}