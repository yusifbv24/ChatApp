using ChatApp.Modules.Identity.Application.Behaviors;
using ChatApp.Modules.Identity.Application.Commands.Login;
using ChatApp.Modules.Identity.Infrastructure;
using ChatApp.Modules.Identity.Infrastructure.Persistence;
using ChatApp.Shared.Infrastructure.EventBus;
using ChatApp.Shared.Infrastructure.Logging;
using ChatApp.Shared.Infrastructure.Middleware;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
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
builder.Services.AddControllers();

// Add CORS policy for local development and production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register MediatR for CQRS pattern
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
    typeof(LoginCommand).Assembly));

// Register FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(LoginCommand).Assembly);

// Add validation behavior to MediatR pipeline
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Register infrastructure layers
builder.Services.AddIdentityInfrastructure(builder.Configuration);

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
        ClockSkew = TimeSpan.Zero // Remove default 5 minute tolerance
    };
});

builder.Services.AddAuthorization();

// Configure Swagger/OpenAPI with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ChatApp Identity API",
        Version = "v1",
        Description = "Identity and Authentication module for ChatApp"
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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Starting database initialization...");

        // Get the DbContext from the service provider
        var context = services.GetRequiredService<IdentityDbContext>();
        await context.Database.MigrateAsync();

        logger.LogInformation("All pending migrations applied successfully");

        // Now seed the database with initial data if needed
        // We only seed if the database is empty (no users exist)
        await DatabaseSeeder.SeedAsync(context, logger);

        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        // In production, you might want to prevent the application from starting if this fails
        logger.LogError(ex, "An error occurred while initializing the database");

        if (app.Environment.IsDevelopment())
        {
            throw; // Re-throw in development to make the error obvious
        }
    }
}

// Configure the HTTP request pipeline

// Global exception handling middleware
app.UseMiddleware<Globalexceptionhandlermiddleware>();

// Request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatApp Identity API v1");
    });
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Log.Information("ChatApp Identity API starting...");

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