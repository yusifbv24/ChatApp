using ChatApp.Shared.Infrastructure.Caching;
using ChatApp.Shared.Infrastructure.EventBus;
using ChatApp.Shared.Infrastructure.Session;
using ChatApp.Shared.Kernel.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Shared.Infrastructure;

/// <summary>
/// Centralized DI registration for all shared infrastructure services.
/// Call AddSharedInfrastructure() from Program.cs instead of registering services inline.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Redis distributed cache (backing store for ICacheService and ISessionStore)
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"]
                ?? "localhost:6379,abortConnect=false";
            options.InstanceName = "ChatApp:";
        });

        // Generic cache service — modullar üçün type-safe Redis caching
        services.AddSingleton<ICacheService, RedisCacheService>();

        // BFF Session Store — opaque session ID → JWT mapping (Redis-backed)
        services.AddSingleton<ISessionStore, RedisSessionStore>();

        // Event Bus — inter-module domain event communication
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        // Memory Cache — lokal in-process cache (ChannelMemberCache kimi sürətli lookups üçün)
        services.AddMemoryCache();

        return services;
    }
}