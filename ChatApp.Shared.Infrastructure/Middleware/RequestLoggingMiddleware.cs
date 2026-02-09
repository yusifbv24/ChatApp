using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ChatApp.Shared.Infrastructure.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopWatch = Stopwatch.StartNew();
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;

            // Redact query string for hub paths to prevent token exposure in logs
            var logPath = requestPath.Value ?? "";
            if (logPath.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase) && context.Request.QueryString.HasValue)
            {
                logPath += " [query redacted]";
            }

            _logger?.LogInformation("Incoming request: {Method} {Path}", requestMethod, logPath);

            try
            {
                await _next(context);
            }
            finally
            {
                stopWatch.Stop();
                var statusCode = context.Response.StatusCode;
                var elapsedMs = stopWatch.ElapsedMilliseconds;

                _logger?.LogInformation(
                    "Completed request: {Method} {Path} - Status: {StatusCode} - Duration: {ElapsedMs}ms",
                    requestMethod,
                    logPath,
                    statusCode,
                    elapsedMs);
            }
        }
    }
}