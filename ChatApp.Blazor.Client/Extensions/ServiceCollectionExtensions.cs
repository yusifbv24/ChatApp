using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.Features.Admin.Services;
using ChatApp.Blazor.Client.Features.Channels.Services;
using ChatApp.Blazor.Client.Features.DirectMessages.Services;
using ChatApp.Blazor.Client.Features.Files.Services;
using ChatApp.Blazor.Client.Features.Files.State;
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

        // Direct Message Services
        services.AddScoped<IDirectConversationService, DirectConversationService>();
        services.AddScoped<IDirectMessageService, DirectMessageService>();

        // File Services
        services.AddScoped<IFileService, FileService>();

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
        services.AddScoped<DirectMessageState>();
        services.AddScoped<FileState>();

        return services;
    }
}
