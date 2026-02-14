using ChatApp.Api.Seeder;
using ChatApp.Modules.Channels.Api.Controllers;
using ChatApp.Modules.Channels.Application.Events;
using ChatApp.Modules.Channels.Domain.Events;
using ChatApp.Modules.Channels.Infrastructure;
using ChatApp.Modules.Channels.Infrastructure.Persistence;
using ChatApp.Modules.DirectMessages.Api.Controllers;
using ChatApp.Modules.DirectMessages.Application.Events;
using ChatApp.Modules.DirectMessages.Infrastructure;
using ChatApp.Modules.DirectMessages.Infrastructure.Persistence;
using ChatApp.Modules.Files.Api.Controllers;
using ChatApp.Modules.Files.Infrastructure;
using ChatApp.Modules.Files.Infrastructure.Persistence;
using ChatApp.Modules.Identity.Api.Controllers;
using ChatApp.Modules.Identity.Domain.Events;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Modules.Identity.Infrastructure;
using ChatApp.Modules.Identity.Infrastructure.Middleware;
using ChatApp.Modules.Identity.Infrastructure.Persistence;
using ChatApp.Modules.Notifications.Api.Controllers;
using ChatApp.Modules.Notifications.Infrastructure;
using ChatApp.Modules.Notifications.Infrastructure.Persistence;
using ChatApp.Modules.Search.Api.Controllers;
using ChatApp.Modules.Search.Infrastructure;
using ChatApp.Modules.Settings.Api.Controllers;
using ChatApp.Modules.Settings.Infrastructure;
using ChatApp.Modules.Settings.Infrastructure.Persistence;
using ChatApp.Shared.Infrastructure;
using ChatApp.Shared.Infrastructure.Authorization;
using ChatApp.Shared.Kernel.Interfaces;
using ChatApp.Shared.Infrastructure.Logging;
using ChatApp.Shared.Infrastructure.Middleware;
using ChatApp.Shared.Infrastructure.SignalR.Hubs;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json;

// Configure Serilog logging
LoggingConfiguration.ConfigureLogging();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    })
    .AddApplicationPart(typeof(AuthController).Assembly)
    .AddApplicationPart(typeof(ChannelsController).Assembly)
    .AddApplicationPart(typeof(DirectConversationsController).Assembly)
    .AddApplicationPart(typeof(FilesController).Assembly)
    .AddApplicationPart(typeof(SearchController).Assembly)
    .AddApplicationPart(typeof(NotificationsController).Assembly)
    .AddApplicationPart(typeof(SettingsController).Assembly);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5300", "http://localhost:5301","null")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });
});

// Register all modules
// Identity Module
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// Channels Module
builder.Services.AddChannelsApplication();
builder.Services.AddChannelsInfrastructure(builder.Configuration);

// DirectMessages Module
builder.Services.AddDirectMessagesApplication();
builder.Services.AddDirectMessagesInfrastructure(builder.Configuration);

// Files Module
builder.Services.AddFilesApplication();
builder.Services.AddFilesInfrastructure(builder.Configuration);


// Search Module
builder.Services.AddSearchApplication();
builder.Services.AddSearchInfrastructure(builder.Configuration);


// Notification Module
builder.Services.AddNotificationsApplication();
builder.Services.AddNotificationsInfrastructure(builder.Configuration);

// Settings Module
builder.Services.AddSettingsApplication();
builder.Services.AddSettingsInfrastructure(builder.Configuration);


// Register shared infrastructure (Redis, cache, session store, event bus)
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Register SignalR services
builder.Services.AddSingleton<IConnectionManager,ConnectionManager>();
builder.Services.AddScoped<IPresenceService,PresenceService>();
builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();
builder.Services.AddSingleton<IChannelMemberCache, ChannelMemberCache>();


// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    // Keep-alive ping interval - server sends ping to client every 15 seconds
    // Reduced from 30s for faster dead connection detection
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    // Client timeout - if no response within 30 seconds, consider disconnected
    // Must be at least 2x KeepAliveInterval (SignalR requirement)
    // Reduced from 2 minutes for faster recovery after sleep/lock
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero
    };

    // Configure JWT to read from session store (BFF pattern) and SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            string? accessToken = null;

            // Priority 1: Read opaque session ID from cookie, resolve real JWT from session store
            var sessionId = context.Request.Cookies["_sid"];
            if (!string.IsNullOrEmpty(sessionId))
            {
                var sessionStore = context.HttpContext.RequestServices.GetRequiredService<ISessionStore>();
                accessToken = sessionStore.GetAccessToken(sessionId);
            }

            // Priority 2: For SignalR, read from query string (cookies not available in WebSocket handshake)
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = context.Request.Query["access_token"];
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// This is where we register our custom authorization components that check permissions
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    // Configure default policy to require authentication
    // This means any endpoint without [AllowAnonymous] requires a valid JWT token
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Fallback policy ensures all endpoints require authentication unless explicitly marked [AllowAnonymous]
    // This is an additional safety net - even if you forget [Authorize] on a controller, authentication is still required
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ChatApp API",
        Version = "v1",
        Description = "Modular Monolith Chat Application API"
    });

    // Custom schema IDs to avoid conflicts in modular monolith
    options.CustomSchemaIds(type => type.FullName);

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Database initialization and seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher>();

    try
    {
        logger.LogInformation("Starting database initialization...");

        // Identity Module
        var identityContext = services.GetRequiredService<IdentityDbContext>();
        await identityContext.Database.MigrateAsync();
        await IdentityDatabaseSeeder.SeedAsync(identityContext, passwordHasher, logger);


        // Channels Module
        var channelsContext = services.GetRequiredService<ChannelsDbContext>();
        await channelsContext.Database.MigrateAsync();
        await ChannelDatabaseSeeder.SeedAsync(channelsContext,logger);


        // DirectMessages Module - ADD THESE LINES
        var directMessagesContext = services.GetRequiredService<DirectMessagesDbContext>();
        await directMessagesContext.Database.MigrateAsync();
        // Create direct conversation for existing default users
        await DefaultSeeder.CreateConversationForDefaultUsers(identityContext, directMessagesContext, logger);


        // Add Files module seeding in database initialization
        var filesContext = services.GetRequiredService<FilesDbContext>();
        await filesContext.Database.MigrateAsync();
        await FileDatabaseSeeder.SeedAsync(filesContext,logger);


        // Notifications Module
        var notificationsContext = services.GetRequiredService<NotificationsDbContext>();
        await notificationsContext.Database.MigrateAsync();
        await NotificationsDatabaseSeeder.SeedAsync(notificationsContext,logger);

        // Settings Module
        var settingsContext = services.GetRequiredService<SettingsDbContext>();
        await settingsContext.Database.MigrateAsync();
        await UserSettingsDatabaseSeeder.SeedAsync(settingsContext,logger);

        logger.LogInformation("Database initialization completed successfully");

        // Subscribe to domain events
        var eventBus = services.GetRequiredService<IEventBus>();

        // Subscribe UserCreatedEvent to create Notes conversation
        eventBus.Subscribe<UserCreatedEvent>(async (@event) =>
        {
            using var handlerScope = app.Services.CreateScope();
            var handler = handlerScope.ServiceProvider.GetRequiredService<UserCreatedEventHandler>();
            await handler.HandleAsync(@event);
        });

        // Subscribe MemberRemovedEvent to notify channel members via SignalR
        eventBus.Subscribe<MemberRemovedEvent>(async (@event) =>
        {
            using var handlerScope = app.Services.CreateScope();
            var handler = handlerScope.ServiceProvider.GetRequiredService<MemberRemovedEventHandler>();
            await handler.HandleAsync(@event);
        });

        logger.LogInformation("Event subscriptions registered successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// Configure the HTTP request pipeline
app.UseMiddleware<Globalexceptionhandlermiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatApp API v1");
    });
}

app.UseHttpsRedirection();

// IMPORTANT: CORS must be before routing for SignalR
app.UseCors("AllowFrontend");

// Configure static files to serve uploaded files from the storage path
var fileStoragePath = builder.Configuration.GetSection("FileStorage")["LocalPath"] ?? "D:\\ChatAppUploads";
if (Directory.Exists(fileStoragePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(fileStoragePath),
        RequestPath = "/uploads"
    });
}
else
{
    Log.Warning($"File storage path does not exist: {fileStoragePath}");
}

app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<UpdateLastVisitMiddleware>();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

Log.Information("ChatApp API starting...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}