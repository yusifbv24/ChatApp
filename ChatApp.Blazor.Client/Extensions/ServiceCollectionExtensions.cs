using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.Features.Admin.Services;
using ChatApp.Blazor.Client.Features.Channels.Services;
using ChatApp.Blazor.Client.State;

namespace ChatApp.Blazor.Client.Extensions;

/// <summary>
/// Extension methods for service registration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all feature services
    /// </summary>
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        // Auth Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        // Admin Services
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();

        // Channel Services
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IChannelMessageService, ChannelMessageService>();
        services.AddScoped<IChannelMemberService, ChannelMemberService>();

        return services;
    }

    /// <summary>
    /// Registers state management services
    /// </summary>
    public static IServiceCollection AddStateManagement(this IServiceCollection services)
    {
        services.AddScoped<AppState>();
        services.AddScoped<UserState>();
        services.AddScoped<ChannelState>();

        return services;
    }
}
