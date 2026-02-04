using ChatApp.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChatApp.Modules.Identity.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware that automatically updates the LastVisit timestamp for authenticated users
    /// This runs asynchronously without blocking the HTTP request
    /// </summary>
    public class UpdateLastVisitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UpdateLastVisitMiddleware> _logger;

        public UpdateLastVisitMiddleware(
            RequestDelegate next,
            IServiceScopeFactory scopeFactory,
            ILogger<UpdateLastVisitMiddleware> logger)
        {
            _next = next;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // First, let the request continue
            await _next(context);

            // Then update LastVisit asynchronously (fire-and-forget)
            // Only for authenticated users and successful responses
            if (context.User?.Identity?.IsAuthenticated == true &&
                context.Response.StatusCode < 400)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    // Fire-and-forget: update LastVisit in background
                    // Use IServiceScopeFactory (singleton) instead of IServiceProvider (request-scoped)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Create a new scope for the background task
                            using var scope = _scopeFactory.CreateScope();
                            var identityContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

                            var user = await identityContext.Users
                                .FirstOrDefaultAsync(u => u.Id == userId);

                            if (user != null)
                            {
                                user.UpdateLastVisit();
                                await identityContext.SaveChangesAsync(CancellationToken.None);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log but don't throw - this is a fire-and-forget operation
                            _logger.LogWarning(
                                ex,
                                "Failed to update LastVisit for user {UserId}",
                                userId);
                        }
                    });
                }
            }
        }
    }
}