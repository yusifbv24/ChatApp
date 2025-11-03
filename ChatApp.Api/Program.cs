using ChatApp.Modules.Channels.Api.Controllers;
using ChatApp.Modules.Channels.Infrastructure;
using ChatApp.Modules.Channels.Infrastructure.Persistence;
using ChatApp.Modules.DirectMessages.Api.Controllers;
using ChatApp.Modules.DirectMessages.Infrastructure;
using ChatApp.Modules.DirectMessages.Infrastructure.Persistence;
using ChatApp.Modules.Identity.Api.Controllers;
using ChatApp.Modules.Identity.Domain.Services;
using ChatApp.Modules.Identity.Infrastructure;
using ChatApp.Modules.Identity.Infrastructure.Persistence;
using ChatApp.Shared.Infrastructure.EventBus;
using ChatApp.Shared.Infrastructure.Logging;
using ChatApp.Shared.Infrastructure.Middleware;
using ChatApp.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

// Configure Serilog logging
LoggingConfiguration.ConfigureLogging();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuthController).Assembly)
    .AddApplicationPart(typeof(ChannelsController).Assembly)
    .AddApplicationPart(typeof(DirectConversationsController).Assembly);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

// Register event bus for inter-module communication
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

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
});

builder.Services.AddAuthorization();

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
        await DatabaseSeeder.SeedAsync(identityContext,passwordHasher,logger);

        // Channels Module
        var channelsContext = services.GetRequiredService<ChannelsDbContext>();
        await channelsContext.Database.MigrateAsync();
        await ChannelDatabaseSeeder.SeedAsync(channelsContext,logger);


        // DirectMessages Module - ADD THESE LINES
        var directMessagesContext = services.GetRequiredService<DirectMessagesDbContext>();
        await directMessagesContext.Database.MigrateAsync();
        await DirectMessagesDatabaseSeeder.SeedAsync(directMessagesContext,logger);

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
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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