using ChatApp.Modules.Channels.Api.Controllers;
using ChatApp.Modules.Channels.Infrastructure;
using ChatApp.Modules.Channels.Infrastructure.Persistence;
using ChatApp.Modules.DirectMessages.Api.Controllers;
using ChatApp.Modules.DirectMessages.Infrastructure;
using ChatApp.Modules.DirectMessages.Infrastructure.Persistence;
using ChatApp.Modules.Files.Api.Controllers;
using ChatApp.Modules.Files.Infrastructure;
using ChatApp.Modules.Files.Infrastructure.Persistence;
using ChatApp.Modules.Identity.Api.Controllers;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Modules.Identity.Infrastructure;
using ChatApp.Modules.Identity.Infrastructure.Persistence;
using ChatApp.Modules.Notifications.Api.Controllers;
using ChatApp.Modules.Notifications.Infrastructure;
using ChatApp.Modules.Notifications.Infrastructure.Persistence;
using ChatApp.Modules.Search.Api.Controllers;
using ChatApp.Modules.Search.Infrastructure;
using ChatApp.Modules.Search.Infrastructure.Persistence;
using ChatApp.Modules.Settings.Api.Controllers;
using ChatApp.Modules.Settings.Infrastructure;
using ChatApp.Modules.Settings.Infrastructure.Persistence;
using ChatApp.Shared.Infrastructure.Authorization;
using ChatApp.Shared.Infrastructure.EventBus;
using ChatApp.Shared.Infrastructure.Logging;
using ChatApp.Shared.Infrastructure.Middleware;
using ChatApp.Shared.Infrastructure.SignalR.Hubs;
using ChatApp.Shared.Infrastructure.SignalR.Services;
using ChatApp.Shared.Kernel.Interfaces;
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


// Register event bus for inter-module communication
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// Register SignalR services
builder.Services.AddSingleton<IConnectionManager,ConnectionManager>();
builder.Services.AddScoped<IPresenceService,PresenceService>();
builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();


// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
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

    // Configure JWT for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for SignalR hub
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
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
        await IdentityDatabaseSeeder.SeedAsync(identityContext,passwordHasher,logger);

        // Channels Module
        var channelsContext = services.GetRequiredService<ChannelsDbContext>();
        await channelsContext.Database.MigrateAsync();
        await ChannelDatabaseSeeder.SeedAsync(channelsContext,logger);


        // DirectMessages Module - ADD THESE LINES
        var directMessagesContext = services.GetRequiredService<DirectMessagesDbContext>();
        await directMessagesContext.Database.MigrateAsync();
        await DirectMessagesDatabaseSeeder.SeedAsync(directMessagesContext,logger);


        // Add Files module seeding in database initialization
        var filesContext = services.GetRequiredService<FilesDbContext>();
        await filesContext.Database.MigrateAsync();
        await FileDatabaseSeeder.SeedAsync(filesContext,logger);

        // Search Module (no migrations - read-only)
        var searchContext = services.GetRequiredService<SearchDbContext>();
        // Search module doesn't need migrations as it only reads from other modules

        // Notifications Module
        var notificationsContext = services.GetRequiredService<NotificationsDbContext>();
        await notificationsContext.Database.MigrateAsync();
        await NotificationsDatabaseSeeder.SeedAsync(notificationsContext,logger);

        // Settings Module
        var settingsContext = services.GetRequiredService<SettingsDbContext>();
        await settingsContext.Database.MigrateAsync();
        await UserSettingsDatabaseSeeder.SeedAsync(settingsContext,logger);

        logger.LogInformation("Database initialization completed successfully");
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